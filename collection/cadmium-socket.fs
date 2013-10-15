#! /usr/bin/gforth

include ../socket.fs
include ../string.fs
include ../gpio/rpi_GPIO_lib.fs

\ warnings off

variable junk$ junk$ $init
777 constant time-exceeded-fail

4445 value sensor-port#

: #to$ ( n -- c-addr u1 ) \ convert n to string 
    s>d
    swap over dabs
    <<# #s rot sign #> #>> ;

: CdS-raw-read { ncadpin -- ntime nflag } \ nflag is 0 if ntime is a real value
    try
	piosetup throw
	ncadpin pipinsetpulldisable throw
	ncadpin pipinoutput throw
	ncadpin pipinlow throw
	2 ms
	ncadpin pipininput throw
	utime
	1000000
	begin
	    1 - dup
	    if
		ncadpin pad pipinread throw
		pad c@
	    else
		drop time-exceeded-fail throw
	    then
	until
	utime rot drop 2swap d- d>s
	piocleanup throw
	false
    restore dup if 0 swap piocleanup drop then
    endtry ;

: cadmium-server ( -- )
    s" Starting server!" type cr
    sensor-port# create-server 0 { lsocket socketid }
    lsocket 1 listen
    begin
        lsocket accept-socket to socketid
        0 0 begin
            2drop
            socketid pad 80 read-socket-from
            2dup s" GET" search swap drop swap drop
	until
\	s" recieved :" type cr
\	2dup type cr
	2dup s" pin" search 
	if
	    rot drop rot drop 
\	    2dup type cr
	    3 - swap 4 + swap s>unumber? false =
	    if
		d>s CdS-raw-read
		if
		    drop s" Sensor read error!" socketid write-socket false
		else
		    s" CV: " junk$ $! #to$ junk$ $+! junk$ $@ socketid write-socket false
		then
	    else
		2drop s" Need pin value!" socketid write-socket false
	    then
	else
	    2drop 
	    s" close server" search swap drop swap drop 
	    if
		s" Server closing!" socketid write-socket true
	    else
		s" No command recieved!" socketid write-socket false
	    then
	then
	socketid close-socket
    until ;

: cadmium-client ( -- caddr u )
\    cr
\    s" Starting client!" type cr
    s" 192.168.0.107" sensor-port# open-socket { socketid }
    s" GET pin 7 " socketid write-socket
    0 0 begin
        2drop
	socketid pad 80 read-socket-from
	dup 4 > 
    until
   \ s" The response is as follows:" type cr
   \ type cr
    socketid close-socket ;

: cadmium-server-close ( -- caddr u )
\    cr
\    s" Sending server close!" type cr
    s" 192.168.0.107" sensor-port# open-socket { socketid }
    s" GET close server" socketid write-socket
    0 0 begin
	2drop
	socketid pad 80 read-socket-from
	dup 4 >
    until
\    s" the response is as follows:" type cr
\    type  cr
    socketid close-socket ;

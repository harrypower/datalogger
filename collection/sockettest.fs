#! /usr/bin/gforth

include ../socket.fs
include ../string.fs

\ warnings off

variable junk$ junk$ $init

4445 value myserver-port#
4445 value sensor-port#

: #to$ ( n -- c-addr u1 ) \ convert n to string then add a "," at the end of the converted string
    s>d
    swap over dabs
    <<# #s rot sign #> #>>
    junk$ $! s" ," junk$ $+! junk$ $@ ;



: datastore ( caddr u -- )
    s" /home/pi/git/datalogger/collection/temphumd.data" w/o open-file throw dup >r
    write-file throw
    r> dup flush-file throw close-file throw ;



: socket-server ( -- )
    s" Starting server!" type cr 
    myserver-port# create-server 0 { lsocket socketid }
    lsocket 1 listen
    begin
	lsocket accept-socket to socketid
	0 0 begin
	    2drop
	    socketid pad 80 read-socket-from
	    2dup s" GET" search swap drop swap drop
	until
	2dup socketid write-socket
	s" Temperature 24 Humidity 35" socketid write-socket
	socketid close-socket
	s" close server" search swap drop swap drop 
    until
;

: socket-client ( -- )
    cr
    s" Starting client!" type cr
    s" 192.168.0.107" sensor-port# open-socket { socketid }
    s" GET" socketid write-socket
    0 0 begin
	2drop
	socketid pad 80 read-socket-from dup 20 >
    until
    s" The response is as follows:" type cr
    type cr
    hostname type cr
    socketid close-socket
;

: server-close ( -- )
    cr
    s" Sending server close!" type cr
    s" 192.168.0.107" sensor-port# open-socket { socketid }
    s" GET close server" socketid write-socket
    0 0 begin
	2drop
	socketid pad 80 read-socket-from dup 20 >
    until
    s" the response is as follows:" type cr
    type  cr
    socketid close-socket
;

\ script? [if] :noname socket-server bye ; is bootmessage [then]
\ script? [if] :noname socket-client bye ; is bootmessage [then]

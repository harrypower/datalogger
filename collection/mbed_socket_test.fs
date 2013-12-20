#! /usr/local/bin/gforth

include ../string.fs
include ../socket.fs
include ../Gforth-Tools/sqlite3_gforth_lib.fs
include errorlogging.fs

decimal 
4446 value mbed-port#

variable mbed-ip$
s" 192.168.0.116" mbed-ip$ $!
0 value buffer
here to buffer 500 allot
variable junk$ junk$ $init
variable temp$ temp$ $init



: #to$ ( n -- c-addr u1 ) \ convert n to string then add a "," at the end of the converted string
    s>d
    swap over dabs
    <<# #s rot sign #> #>>
        junk$ $! s" ," junk$ $+! junk$ $@ ;

: mbedread-client ( caddr u nport# -- caddr1 u1 )
    \ s" Start client!" type cr
    open-socket { socketid }
    s\" GET /val \r\n\r\n" socketid write-socket
    0 0 begin
	2drop
 	socketid buffer 499 read-socket  \ note read-socket is for TCP read-socket-from is for UDP
	2dup s\" \r\n\r\n" search swap drop swap drop
	\ this will bail if it finds end cr lf cr lf or it will generate error 
    until
    socketid close-socket ;

: readmany (  -- )
    10 0 ?do
	mbed-ip$ $@ mbed-port# mbedread-client  ." total from mbed: " dup . cr type cr
	4000 ms \ wait 4 seconds
    loop
;

: read120 ( -- )
    s" 192.168.0.120" 4446 mbedread-client ." total: " dup . cr cr type cr ;

: read116 ( -- )
    s" 192.168.0.116" 4446 mbedread-client ." total: " dup . cr cr type cr ;

: get-thp$ ( -- caddr u nflag )  \ reads the mbed server and returns the data to be inserted into db
    try   \ flag is false for reading of mbed was ok true means reading failed for some reason
	mbed-ip$ $@ mbed-port# mbedread-client
	s" HTTP/1.0 200 OK" search
	if
	    s\" \r\n\r\n" search
	    if
		2dup 4 + s\" \r\n\r\n" search
		if 2drop 8 - swap 4 + swap false else 2drop 2drop true then
	    else
		2drop true
	    then
	else
	    2drop true
	then
    restore dup if 0 swap 0 swap then
    endtry ;

: ck-thp$ ( caddr u -- nflag )  \ looks at string to see if all data is present by finding 4 comma's 
    0 { count }  \ if 4 comma's are found then false is returned true is returned if any other value is found
    0 ?do
	dup c@ ',' = if count 1+ to count then 1+
    loop drop
    count 4 = if false else true then ;


: !data ( caddr u -- )
    s" atest.data" dbname
    s" insert into thpdata values(" temp$ $!
    datetime datetime$ $@ temp$ $+!
    temp$ $+!
    s" );" temp$ $+!
    temp$ $@ dbcmds
    sendsqlite3cmd throw
;

: gcs-thps$ ( -- )
    get-thp$ 0 =
    if
	{ caddr u } caddr u
	ck-thp$ 0 =
	if  \ data from mbed checks out now put in database
	    caddr u !data
	then
    else
	2drop \ this is some error so maybe store that!
    then ;

: createdb ( -- )
    s" atest.data" dbname
    s" CREATE TABLE IF NOT EXISTS thpdata(year int,month int,day int, hour int, min int, sec int, age int,temp int,humd int,btemp int, pressure int);" dbcmds
    sendsqlite3cmd throw ;

: see-db ( -- caddr u )
    s" atest.data" dbname
    s" select * from thpdata;" dbcmds
    sendsqlite3cmd throw
    dbret$ ;


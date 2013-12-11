#! /usr/local/bin/gforth

include ../string.fs
include ../socket.fs

4446 value mbed-port#

variable mbed-ip$
s" 192.168.0.116" mbed-ip$ $!
0 value buffer
here to buffer 500 allot


: mbedread-client ( caddr u nport# -- caddr1 u1 )
    \ s" Start client!" type cr
    open-socket { socketid }
    s\" GET /read \r\n\r\n" socketid write-socket
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


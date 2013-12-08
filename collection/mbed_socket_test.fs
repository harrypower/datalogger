#! /usr/local/bin/gforth

include ../string.fs
include ../socket.fs

4446 value mbed-port#

variable mbed-ip$
s" 192.168.0.116" mbed-ip$ $!
0 value buffer
here to buffer 500 allot


: mbedread-client ( -- caddr u )
    s" Start client!" type cr
    mbed-ip$ $@ mbed-port# open-socket { socketid }
    s\" GET /read \r\n\r\n" socketid write-socket
    0 0 begin
    	2drop
	socketid buffer 499 read-socket  \ note read-socket is for TCP read-socket-from is for UDP
    	dup 10 >
    until
    socketid close-socket ;

: readmany ( -- )
    10 0 ?do
	mbedread-client type cr
	4000 ms \ wait 4 seconds
    loop
;


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
    s\" GET / \r\n\r\n" socketid write-socket
    0 0 begin
    	2drop
	socketid buffer 499 read-socket-from
    	dup 1 >
    until
    socketid close-socket ;



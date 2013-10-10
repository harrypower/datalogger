#! /usr/bin/gforth

include ../socket.fs
include ../string.fs

\ warnings off

variable junk$ junk$ $init

4445 value myserver-port#

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
    s" Scripting is starting server!" type cr 
    myserver-port# create-server 0 { lsocket socketid }
    lsocket 1 listen
    lsocket accept-socket to socketid
    socketid pad 80 read-socket 2dup datastore
    2dup type cr
    socketid write-socket
    s" Got it!" socketid write-socket
    hostname type cr
    socketid close-socket
;

script? [if] :noname socket-server bye ; is bootmessage [then]
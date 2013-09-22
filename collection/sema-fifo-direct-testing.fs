#! /usr/local/bin/gforth

include ../string.fs
include cadmium-svr-test.fs
include script.fs
include cadmium-sensor2.fs

: time-test
    utime
    send-read-get$ drop 2drop \ . type cr
    utime 2swap d-
;

: dotime-test
    0 0 50 0 ?do time-test d+ loop 2dup d. 1 51 m*/ d. ;


: time-test1
    utime
    7 CdS-raw-read 2drop \ . . cr
    utime 2swap d-
;

: dotime-test1
    0 0 50 0 ?do time-test1 d+ loop 2dup d. 1 51 m*/ d. ;


: time-test2
    utime
    s" ../gpio/cadmium-sensor.fs 7" shget throw 2drop \ type cr
    utime 2swap d- ;

: dotime-test2
    0 0 50 0 ?do time-test2 d+ loop 2dup d. 1 51 m*/ d. ;




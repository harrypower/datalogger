#! /usr/bin/gforth

include ../string.fs
include cadmium-sensor-test.fs

variable junk$

: makepipe ( -- )
    s" sudo mkfifo -m 777 /var/tmp/gforth-tmp-pipe" system $? throw ;

: close-mypipe ( -- )
    s" sudo rm /var/tmp/gforth-tmp-pipe" system $? throw ;

: read-sensor ( caddr u -- caddr1 u1 nflag ) \ nflag is zero for sensor correctly read.
    \ realize this code has blocking elements so the speed of the sensor is the limit
    try
	{ caddr u }
	s" sudo " junk$ $! caddr u junk$ $+! s"  > /var/tmp/gforth-tmp-pipe &" junk$ $+! junk$ $@ system $? throw
	s" /var/tmp/gforth-tmp-pipe" r/o open-file throw
	pad swap 80 swap read-file throw pad swap
	false
    restore  
    endtry ;


: time_test
    utime
    makepipe
    s" /home/pi/git/datalogger/gpio/cadmium-sensor.fs 7" read-sensor throw 2drop \ type cr
    close-mypipe
    utime 2swap d- 
;

\ this is test for opening a pipe then compiling code and reading sensor and closeing pipe

: dotime-test
    0 0 50 0 ?do time_test d+ loop 2dup d. 1 51 m*/ d. ;

: time-test1
    utime
    s" /home/pi/git/datalogger/gpio/cadmium-sensor.fs 7" read-sensor throw 2drop \ type cr
    utime 2swap d- ;

\ This test is opening and closing pipe once but code is recompiled each sensor read

: dotime-test1
    makepipe
    0 0 50 0 ?do time-test1 d+ loop 2dup d. 1 51 m*/ d.
    close-mypipe ;

: maketemp
    s" sudo touch /var/tmp/gforth-tmp-file" system $? throw
    s" sudo chmod 777 /var/tmp/gforth-tmp-file" system $? throw ;


: close-temp
    s" sudo rm /var/tmp/gforth-tmp-file" system $? throw ;

: read-sensor-file ( caddr u -- caddr1 u1 nflag ) \ nflag is zero for sensor crrectly read.
    try
	{ caddr u }
	s" sudo " junk$ $! caddr u junk$ $+! s"  > /var/tmp/gforth-tmp-file" junk$ $+! junk$ $@ system $? throw
	s" /var/tmp/gforth-tmp-file" slurp-file
	false
    restore
    endtry ;
	
: time-test2
    utime
    maketemp
    s" /home/pi/git/datalogger/gpio/cadmium-sensor.fs 7" read-sensor-file throw 2drop \ type cr
    close-temp
    utime 2swap d- ;

\ This test is making file compiling code sending output to file closing file

: dotime-test2
    0 0 50 0 ?do time-test2 d+ loop 2dup d. 1 51 m*/ d. ;

: time-test3
    utime
    s" /home/pi/git/datalogger/gpio/cadmium-sensor.fs 7" read-sensor-file throw type cr
    utime 2swap d- ;

\ This test is opening and closing file once but recompiling code then reading sensor each time
: dotime-test3
    maketemp
    0 0 50 0 ?do time-test3 d+ loop 2dup d. 1 51 m*/ d. 
    close-temp ;

\  This is direct compiled code execution with no pipe or file used 
: read-sensor-direct
    try
	7 cds-raw-read throw
    restore
    endtry ;

: time-test4
    utime
    read-sensor-direct . cr
    utime 2swap d- ;

: dotime-test4
    0 0 50 0 ?do time-test4 d+ loop 2dup d. 1 51 m*/ d. ;

\ :noname defers 'cold dotime-test3 cr dotime-test4 cr threading-method . cr version-string type cr bye ; is 'cold
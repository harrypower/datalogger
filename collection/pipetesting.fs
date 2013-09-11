#! /usr/bin/gforth

include ../string.fs
include cadmium-sensor-test.fs
include script.fs

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
    read-sensor-direct drop \ . cr
    utime 2swap d- ;

: dotime-test4
    0 0 50 0 ?do time-test4 d+ loop 2dup d. 1 51 m*/ d. ;

: read-sensor-c ( -- )
    s" /home/pi/git/datalogger/collection/cadmium" read-sensor-file throw 2drop \ type cr
;

: time-test5
    utime
    read-sensor-c 
    utime 2swap d- ;

: dotime-test5
    maketemp 
    0 0 50 0 ?do time-test5 d+ loop 2dup d. 1 51 m*/ d.
    close-temp ;

: read-cad-pipe-direct ( -- )
    s" sudo /home/pi/git/datalogger/collection/cadmium" shget throw 2drop \ type cr
;

: time-test6
    utime
    read-cad-pipe-direct
    utime 2swap d- ;

: dotime-test6
    0 0 50 0 ?do time-test6 d+ loop 2dup d. 1 51 m*/ d. ;

: read-cad-fifo-direct ( -- )
    s" cadmium-cmd" w/o open-file throw s" read" rot dup >r write-file r> close-file throw throw
    s" cadmium-value" r/o open-file throw >r pad 10 r> dup >r read-file throw r> close-file throw pad swap 2drop \ type cr
;

: time-test7 ( -- )
    utime
    read-cad-fifo-direct 
    utime 2swap d- ;

: dotime-test7
    0 0 50 0 ?do time-test7 d+ loop 2dup d. 1 51 m*/ d. ;

: read-cad-svr ( - )
    s" sudo echo read > /var/lib/datalogger-gforth/cadmium_cmd" system $? throw
    s" sudo cat /var/lib/datalogger-gforth/cadmium_value" shget throw 2drop \ type cr
;

: time-test8 ( -- )
    utime
    read-cad-svr
    utime 2swap d- ;

: dotime-test8
    s" sudo /home/pi/git/datalogger/collection/cadmium-svr.fs &" system $? throw
    2000 ms
    0 0 50 0 ?do time-test8 d+ loop 2dup d. 1 51 m*/ d.
    s" sudo echo end > /var/lib/datalogger-gforth/cadmium_cmd" system $? throw ;

: read-cad-svr-direct ( -- )
    s" /var/lib/datalogger-gforth/cadmium_cmd" w/o open-file throw   
    s" read" rot dup >r write-file r> close-file throw throw
    s" /var/lib/datalogger-gforth/cadmium_value" r/o open-file throw >r pad 10 r> dup >r read-file throw r> close-file throw drop \ pad swap type cr
;    

: time-test9 ( -- )
    utime
    read-cad-svr-direct
    utime 2swap d- ;

: dotime-test9
    s" sudo /home/pi/git/datalogger/collection/cadmium-svr.fs &" system $? throw
    3000 ms
    0 0 50 0 ?do time-test9 d+ loop 2dup d. 1 51 m*/ d.
    s" sudo echo end > /var/lib/datalogger-gforth/cadmium_cmd" system $? throw ;

\ :noname defers 'cold dotime-test3 cr dotime-test4 cr threading-method . cr version-string type cr bye ; is 'cold
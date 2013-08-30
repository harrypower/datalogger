#! /usr/bin/gforth

include ../string.fs

variable junk$

: makepipe ( -- )
    s" sudo mkfifo -m 666 /var/tmp/gforth-tmp-pipe" system $? throw ;

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



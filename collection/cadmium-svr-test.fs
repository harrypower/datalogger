#! /usr/bin/gforth

include /home/pi/git/datalogger/string.fs

variable cmd-fifo$
variable value-fifo$
variable fifo-path$
variable junk$

s" /var/lib/datalogger-gforth/" fifo-path$ $!
s" cadmium_cmd" cmd-fifo$ $!
s" cadmium_value" value-fifo$ $!

: shget ( caddr u -- caddr1 u1 nflag )  \ nflag is false if caddr1 and u1 are valid messages from system
    try
	r/o open-pipe throw dup >r slurp-fid
	r> close-pipe throw to $?
	false
    restore
    endtry ;

: filetest ( caddr u -- nflag ) \ nflag is false if the file is present
    try junk$ $init
	s" test -e " junk$ $! junk$ $+! s"  && echo 'yes' || echo 'no'" junk$ $+! junk$ $@ shget throw
	s" no" search
    restore swap drop swap drop
    endtry ;

: cmd-path$ ( -- caddr u ) \ path for command fifo
    junk$ $init
    fifo-path$ $@ junk$ $! cmd-fifo$ $@ junk$ $+! junk$ $@ ;

: value-path$ ( -- caddr u ) \ path to value fifo
    junk$ $init
    fifo-path$ $@ junk$ $! value-fifo$ $@ junk$ $+! junk$ $@ ;


: start-cad-svr ( -- )
    s" sudo /home/pi/git/datalogger/collection/cadmium-svr.fs &" system $? throw
    utime begin 2dup utime 2swap d- 50000 0 d> if -38 throw then  cmd-path$ filetest false = until 2drop 
    utime begin 2dup utime 2swap d- 50000 0 d> if -38 throw then  value-path$ filetest false = until 2drop
;

: close-cad-svr ( -- )
    cmd-path$ junk$ $! $init s" sudo echo end > " junk$ $! junk$ $+! system $? throw ;

: read-cad-svr ( -- caddr u )
    cmd-path$  w/o open-file throw
    s" read 7" rot dup >r write-file r> close-file throw throw
    value-path$ r/o open-file throw >r pad 20 r> dup >r read-file throw r> close-file throw pad swap ;

    
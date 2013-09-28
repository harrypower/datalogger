#! /usr/local/bin/gforth

include /home/pi/git/datalogger/string.fs
include /home/pi/git/datalogger/collection/semaphore.fs

variable cmd-fifo$ cmd-fifo$ $init
variable value-fifo$ value-fifo$ $init
variable fifo-path$ fifo-path$ $init
variable junk$ junk$ $init
variable mysem_t*
variable SEM_FAILED-sem_t*
variable sema-name$ sema-name$ $init
variable sema-system-path$ sema-system-path$ $init

s" /dev/shm/sem." sema-system-path$ $!
s" cadmium-available" sema-name$ $!
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

: sema-path$ ( -- caddr u )
    junk$ $init
    sema-system-path$ $@ junk$ $! sema-name$ $@ junk$ $+! junk$ $@ ;

: open-sema ( -- )
    sema-name$ $@ semaphore-op-existing throw
    mysem_t* ! ;

: close-sema ( -- )
    mysem_t* @ semaphore-constants swap drop <>
    if  mysem_t* @ semaphore-close throw
    then ;

: cadmium-srv-running? ( -- nflag ) \ nflag is false if there is no cadmium srv software running
    sema-path$ filetest             \ nflag is true if the cadmium-srv is running
    if false else true then ;
    
: start-cad-svr? ( -- )
    cadmium-srv-running? false =
    if
	s" sudo /home/pi/git/datalogger/collection/cadmium-svr.fs &" system $? throw
	200 ms
    then ;

: close-cad-svr ( -- )
    cmd-path$ w/o open-file throw
    s" end" rot dup >r write-file r> close-file throw throw
;

: read-cad-svr ( -- caddr u )
    cmd-path$  w/o open-file throw
    s" read" rot dup >r write-file r> close-file throw throw
    value-path$ r/o open-file throw >r pad 80 r> dup >r read-file throw r> close-file throw pad swap ;

: send-read-get$ ( -- caddr u nflag ) \ nflag is false if string was returned from server
    try
	start-cad-svr?
	cadmium-srv-running?
	if
	    open-sema
	    mysem_t* @ semaphore- throw
	    close-sema
	    read-cad-svr
	else
	    0 0 
	then
	false
    restore  dup if 0 0 rot then
    endtry ;


: send-end ( -- nflag ) \ nflag is false if the end command was sent or if server not running do nothing
    try
	cadmium-srv-running?
	if
	    open-sema
	    mysem_t* @ semaphore- throw
	    close-sema
	    close-cad-svr
	then
	false
    restore 
    endtry ;

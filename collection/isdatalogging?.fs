#! /usr/bin/gforth
\ error 11 -- system restarted because of some system or gforth error that should be recorded just before this error in error.data file
\ error 10 -- system restarted due to logger.fs having to be restarted 3 times now
\ error 9  -- logger.fs was restarted as it was detected to not be running

include ../string.fs
include errorlogging.fs
include script.fs

false value pass
true value fail
0 value times_restarted

: isitlogging ( -- )
    TRY s" pgrep logger.fs" shget 0= if swap drop 0= if  false else true then else drop drop false then
	if  pass else fail then
    RESTORE if times_restarted 1 + to times_restarted s" Starting up logger.fs" type cr
	    s" sudo nohup /home/pi/git/datalogging/collection/logger.fs > /home/pi/git/datalogging/collection/logging_restart_msg.data &" system then
    ENDTRY ;

: loggingcheckloop ( -- )
    TRY
	begin
	    60000 ms
	    isitlogging
	    times_restarted 3 >= if 10 error_log s\" sudo shutdown \"now\" -r &" system then
	again
    RESTORE error_log 11 error_log s\" sudo shutdown \"now\" -r &" system
    ENDTRY ;

loggingcheckloop

bye


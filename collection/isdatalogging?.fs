#! /usr/bin/gforth
\ error 311 -- system restarted because of some system or gforth error that should be recorded just before this error in error.data file
\ error 310 -- system restarted due to logger.fs having to be restarted 3 times now
\ error 309  -- logger.fs was restarted as it was detected to not be running

include ../string.fs
include errorlogging.fs
include script.fs

false value pass
true value fail
0 value times_restarted

311 constant loggingcheckloop_error
310 constant logger_error
309 constant logger_not_running

: isitlogging ( -- )
    TRY s" pgrep logger.fs" shget 0= if swap drop 0= if  false else true then else drop drop false then
	if  pass else fail then
    RESTORE if times_restarted 1 + to times_restarted s" Starting up logger.fs" type cr
	    logger_not_running error_log
	    s" sudo nohup /home/pi/git/datalogger/collection/logger.fs > /home/pi/git/datalogger/collection/logging_restart_msg.data &" system then
    ENDTRY ;

: loggingcheckloop ( -- )
    TRY
	begin
	    60000 ms
	    isitlogging
	    times_restarted 3 >= if logger_error error_log s\" sudo shutdown \"now\" -r &" system then
	again
    RESTORE error_log loggingcheckloop_error error_log s\" sudo shutdown \"now\" -r &" system
    ENDTRY ;

loggingcheckloop

bye


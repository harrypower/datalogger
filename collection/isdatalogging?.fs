#! /usr/bin/gforth
\ error 311 -- system restarted because of some system or gforth error that should be recorded just before this error in error.data file
\ error 310 -- system restarted due to logger.fs having to be restarted 3 times now
\ error 309  -- logger.fs was restarted as it was detected to not be running
\ error 312 -- could not resolve absoulute path!

include ../string.fs
include errorlogging.fs
include script.fs

false value pass
true value fail
0 value times_restarted

variable logger_path$
variable logger_msg_path$
variable datalogger_project_path$

s" /collection/logger.fs" logger_path$ $!  \ this will be resolved correctly at run time
s" /collection/logging_restart_msg.data" logger_msg_path$ $! \ this will be resolved correctly at run time

311 constant loggingcheckloop_error
310 constant logger_error
309 constant logger_not_running
312 constant absolute_path_fail

: isitlogging ( -- )  \ this just looks for a process with logger.fs in the name.  Now this may not work perfectly!
    TRY s" pgrep logger.fs" shget 0= if swap drop 0= if  false else true then else drop drop false then
	if  pass else fail then
    RESTORE if times_restarted 1 + to times_restarted s" Starting up logger.fs" type cr
	    logger_not_running error_log
	    s" sudo nohup " junk$ $! logger_path$ $@ junk$ $+! s"  > " junk$ $+! logger_msg_path$ junk$ $+! s"  &" junk$ $+! junk$ $@ system then
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

s" ../datalogger_home_path" filetest
[if] s" ../datalogger_home_path" slurp-file datalogger_project_path$ $!
    datalogger_project_path$ $@ junk$ $! logger_path$ $@ junk$ $+! junk$ $@ logger_path$ $!
    datalogger_project_path$ $@ junk$ $! logger_msg_path$ $@ junk$ $+! junk$ $@ logger_msg_path$ $!
    loggingcheckloop bye
[else] ." Could not resolve absolute path so isdatalogging?.fs is shutting down!" bye
[then]

\ loggingcheckloop




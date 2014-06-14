#! /usr/bin/gforth

\ This code will send a email if some sensor issue is detected
include ../string.fs
include script.fs
include ../Gforth-Tools/sqlite3_gforth_lib.fs

variable junk$
variable msg-smtp$
s" msg.txt" msg-smtp$  $!  \ the file name for r the smtp send  message
variable msg-content$ msg-content$ $init
0 value msg-hnd
variable datalogger_path$
1200 constant thresholdtime \ time in seconds used to detect if email should be sent or not!

s" /var/lib/datalogger-gforth/datalogger_home_path" slurp-file datalogger_path$ $!
s" /collection/sensordb.data" datalogger_path$ $@ junk$ $! junk$ $+!
junk$ $@ dbname
junk$ $init

: filetest ( caddr u -- nflag )  \ looks for the full path and file name in string and returns true if found
    s" test -e " junk$ $! junk$ $+! s"  && echo 'yes' || echo 'no'" junk$ $+! junk$ $@ shget throw 1 -  s" yes" compare  
    if false
    else true
    then ;

: #to$ ( n -- c-addr u1 ) \ convert n to string
    s>d
    swap over dabs
    <<# #s rot sign #> #>> ;

: make-msg ( -- ) \ this will make a file called msg.txt 
    msg-smtp$ $@ filetest false =
    if
	msg-smtp$ $@ w/o create-file throw to msg-hnd
    else
	msg-smtp$ $@ w/o open-file throw to msg-hnd
    then
    s" TO: philipkingsmith@gmail.com" msg-hnd write-line throw
    s" From: thpserver@home.com" msg-hnd write-line throw
    s" Subject: Errors on THPserver possible problems!" msg-hnd write-line throw
    s"  " msg-hnd write-line throw
    s" The last data is: " msg-hnd write-line throw
    msg-content$ $@ msg-hnd write-line throw
    msg-hnd flush-file throw
    msg-hnd close-file throw ;

: send-smtp ( -- )
    s" ssmtp philipkingsmith@gmail.com < msg.txt" system ;

: lastdatatime ( -- nseconds nerror ) \ nseconds last raw data recieved in seconds.
    \ nerror is true if nseconds is valid.
    \ nerror is false if nseconds is not valid for some reason
    s"  " dbfieldseparator
    s"  " dbrecordseparator
    s" select dtime from thpdata limit 1 offset ((select max(row) from thpdata) -1 ) ;" dbcmds    
    sendsqlite3cmd false =
    if
	dbret$ s>number? false =
	if
	    d>s utime 1000000 um/mod swap drop swap - true
	else
	    0 false
	then
    else
	0 false
    then ;

: get-data-msg-content ( -- ) \ collect data from database for sending to email
    s" ," dbfieldseparator
    s\" \n" dbrecordseparator
    s" select row,datetime(dtime,'unixepoch','localtime'),age,DTHtemperature,DTHhumd,BMPtemperature,BMPpressure from thpdata limit 1 offset ((select max(row) from thpdata) -1) ;" dbcmds
    sendsqlite3cmd false =
    if
	s\" Data from collection process! \n" msg-content$ $+!
	dbret$ msg-content$ $+! 
    else
	s\" Error from sqlite3! \n" msg-content$ $+!
	dberrmsg #to$ s" # " msg-content$ $+! msg-content$ $+! msg-content$ $+!
    then ;

: get-error-msg-content ( -- ) \ collect data from database for sending to email
    s" ," dbfieldseparator
    s\" \n" dbrecordseparator
    s" select row,datetime(dtime,'unixepoch','localtime'),error from errors limit 3 offset ((select max(row) from errors) -3) ;" dbcmds
    sendsqlite3cmd false =
    if
	s\" Errors from collection process! \n" msg-content$ $+!
	dbret$ msg-content$ $+! 
    else
	s\" Error from sqlite3! \n" msg-content$ $+!
	dberrmsg #to$ s" # " msg-content$ $+! msg-content$ $+! msg-content$ $+!
    then ;

: check&send ( -- ) \ looks for problems and sends an email if found some
    lastdatatime false =
    if
	drop
	s\" Could not get last data from sqlite3! \n" msg-content$ $!
	get-data-msg-content
	get-error-msg-content
	make-msg
	send-smtp
    else
	thresholdtime >
	if
	    msg-content$ $init
	    get-data-msg-content
	    get-error-msg-content
	    make-msg
	    send-smtp
	then
    then ;


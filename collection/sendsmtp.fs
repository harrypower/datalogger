#! /usr/bin/gforth

\ This Gforth code is a Raspberry Pi smtp email message sending code
\    Copyright (C) 2014  Philip K. Smith

\    This program is free software: you can redistribute it and/or modify
\    it under the terms of the GNU General Public License as published by
\    the Free Software Foundation, either version 3 of the License, or
\    (at your option) any later version.

\    This program is distributed in the hope that it will be useful,
\    but WITHOUT ANY WARRANTY; without even the implied warranty of
\    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\    GNU General Public License for more details.

\    You should have received a copy of the GNU General Public License
\    along with this program.  If not, see <http://www.gnu.org/licenses/>.
\
\ This code detects errors with the data logging software and will
\ send an email if a problem is found.  The email contains info about 
\ the possible problem.  Note the ssmtp is used to send the email so
\ this smtp service needs to be setup used this code.  The code is 
\ run via a cron job that is also set up externally.

warnings off

require string.fs
require script.fs
require ../Gforth-Tools/sqlite3_gforth_lib.fs

variable junk$
variable msg-smtp$
variable msg-content$ s" " msg-content$ $!
0 value msg-hnd
variable datalogger_path$
variable project_path$

1200 constant thresholdtime \ time in seconds used to detect if email should be sent or not!

s" /var/lib/datalogger-gforth/datalogger_home_path" slurp-file project_path$ $!
s" /collection/sensordb.data" project_path$ $@ datalogger_path$ $! datalogger_path$ $+!
s" /collection/msg.txt" project_path$ $@ msg-smtp$ $! msg-smtp$ $+!

datalogger_path$ $@ dbname

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
	msg-smtp$ $@ r/w create-file throw to msg-hnd
    else
	msg-smtp$ $@ r/w open-file throw to msg-hnd
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
    s" sudo ssmtp philipkingsmith@gmail.com < " junk$ $!
    msg-smtp$ $@ junk$ $+!
    junk$ $@
    system ;

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
    s\"  \n" dbrecordseparator
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
    s\"  \n" dbrecordseparator
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
	    s" " msg-content$  $!
	    get-data-msg-content
	    get-error-msg-content
	    make-msg
	    send-smtp
	then
    then ;

: testsend ( -- ) \ used to send data via email to test system
    lastdatatime false =
    if
	drop
	s\" Could not get last data from sqlite3! \n" msg-content$ $!
	get-data-msg-content
	get-error-msg-content
	make-msg
	send-smtp
    else
	drop 
	s" " msg-content$ $!
	get-data-msg-content
	get-error-msg-content
	make-msg
	send-smtp
    then ;

testsend bye
\ check&send bye

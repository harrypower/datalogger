\ #! /usr/local/bin/gforth   \ note this should call gforth version 0.7.2 and up  /usr/bin/gforth  would call 0.7.0 only!
\ this may not be needed because this code is allways reqired by other code 

\ This Gforth code is a Raspberry Pi Data logging code
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

\ This code is the sqlite3 interface type words for this database project
\ What should be in here are words to put in and take out of database!


\ warnings off

require string.fs
require ../Gforth-Tools/sqlite3_gforth_lib.fs
require gforth-misc-tools.fs

decimal

variable db-path$ 
variable temp$

100 constant sqlite3-resend-time    \ this codes sqlite3 routines will wait for this time in ms if a locked database is found befor resending cmds

path$ $@ db-path$ $! s" /collection/datalogged.data" db-path$ $+!  \ this is the name of the database

\ These are the enumerated errors this code can produce
next-exception @ constant sqlite-errorListStart  \ this is start of enumeration of errors for this code

s" Registration string not present in create-datalogging-table!"                 exception constant dltable-name-er
s" Registration string empty or formed incorrectly in parse-new-device-xml!"     exception constant parse-new-er
s" Table name already present in database file! (change name to register)"       exception constant table-present-er
s" No data node's present when making table registration!"                       exception constant no-data-node-er

next-exception @ constant sqlite-errorListEnd    \ this is end of enumeration of errors for this code

: setupsqlite3 ( -- ) \ sets default stuff up for sqlite3 work
    initsqlall
    db-path$ $@ dbname ;

: dberrorthrow ( nerror -- ) \ locked database type errors and buffer overflow error will cause a wait and a resend
    \ if a resend sqlite3 cmd fails then that error is thrown
    \ all other errors will throw that are behond the scope of recovery in this code!
    case
	5     of sqlite3-resend-time ms sendsqlite3cmd dup throw  endof \ database is locked now
	6     of sqlite3-resend-time ms sendsqlite3cmd dup throw  endof \ table is locked now
	sqlerrors retbuffover-err @
	      of sqlmessg retbuffmaxsize-cell @ 2 * mkretbuff sendsqlite3cmd dup throw  endof \ buffer overflow from sqlite3
	\ might want to limit this buffer resize to 10k bytes or something like that
	dup throw
    endcase ;

: sqlite-table? ( caddr u -- nflag ) \ will search db for the string as a table name.  nflag will return false if no table named exists
    try  \ note this will find the string as a sub name in longer table names as a table so name your tables well!
	setupsqlite3  
	s" select name from sqlite_master;" dbcmds
	sendsqlite3cmd dberrorthrow  \ note if some error happens in file access or sqlite3 access this will look like table is present
	dbret$ 2swap search
    restore swap drop swap drop 
    endtry ;

: sqlite-ip? ( caddrip u -- nflag ) \ search device table for the ip addr in the string.  nflag is false if no ip addr registered yet
    setupsqlite3
    s" " dbfieldseparator
    s" " dbrecordseparator
    s" select case when ip = '" temp$ $! temp$ $+! s" ' then -1 else 0 end from devices ;" temp$ $+! temp$ $@ dbcmds
    sendsqlite3cmd dberrorthrow
    dbret$ s>number? false = throw d>s ;

: create-device-table ( -- ) \ This creates the main table used in the database. This table is used to reference all other tables and then data in database.
    \ table is called devices
    \ row is primary key and is autoincrmented
    \ dt_added is the date time stamp device was added
    \ ip address of the device
    \ port number of the device
    \ method would contain the string to send socket device to get data from device eg "val"
    \ data_table is the name of the table that contains the logged data for this registered device
    \ read_device is yes or no.  no means device is not read anymore. yes means device is read from still.
    \ store_data is yes or no. no means data_list_id table is not to be writen to anymore. yes means data_list_id table is to be writen to still when device is read from.
    setupsqlite3
    s" CREATE TABLE IF NOT EXISTS devices(row INTEGER PRIMARY KEY AUTOINCREMENT,dt_added INTEGER," temp$ $!
    s" ip TEXT,port TEXT,method TEXT,data_table TEXT," temp$ $+!
    s" read_device TEXT,store_data TEXT );" temp$ $+! temp$ $@
    dbcmds
    sendsqlite3cmd dberrorthrow ;

: create-error-tables ( -- ) \ used to create the error logging tables 
    setupsqlite3
    s" CREATE TABLE IF NOT EXISTS errors(row INTEGER PRIMARY KEY AUTOINCREMENT,dtime INTEGER,error INT);" dbcmds
    sendsqlite3cmd dberrorthrow
    setupsqlite3
    s" CREATE TABLE IF NOT EXISTS errorList(error INT UNIQUE,errorText TEXT);" dbcmds
    sendsqlite3cmd dberrorthrow ;

: error-sqlite3! ( nerror -- ) \ used to store error values into errors table
    setupsqlite3
    s" insert into errors values(NULL," temp$ $!
    datetime$ temp$ $+!
    #to$ temp$ $+!
    s" );" temp$ $+!
    temp$ $@ dbcmds
    sendsqlite3cmd  dberrorthrow ;

: (errorINlist?) { nsyserror -- nflag ndberror }  \ see if nsyserror number is in database list of errors
    \ ndberror is false only if there was no dberror if ndberror is true then nflag is undefined
    \ nflag is true if nsyserror is in the dbase
    \ nflag is false if nsyserror is not in the dbase
    setupsqlite3
    s" select exists (select error from errorList where (error = " temp$ $!
    nsyserror #to$ temp$ $+!
    s" ));" temp$ $+! temp$ $@ dbcmds
    5 1 ?do 
	sendsqlite3cmd 0 = if leave else i 20 * ms then
    loop
    dbret$ s" 1" search swap drop swap drop dup
    dbret$ s" 0" search swap drop swap drop xor false =
    if
	true
    else
	dberrmsg swap drop swap drop
    then ;

: (puterrorINlist) ( nerror -- ) \ add nerror number and string to the error list
    setupsqlite3
    dup -2 <>  \ this is needed because -2 error does not report a message even though it is Abort !
    if
	s" insert into errorList values(" temp$ $!
	dup  #to$, temp$ $+! s\" \"" temp$ $+!
	error#to$ temp$ $+! s\" \");" temp$ $+! temp$ $@ dbcmds
    else
	drop
	s\" insert into errorList values(-2, \'Abort\" has occured!\');" dbcmds
    then
    sendsqlite3cmd drop \ ****note if there is a sqlite3 error here this new error will not be stored in list*****
    \ **** positive error numbers can come from sqlite3 but the proper string may not get placed in db for the number
;

: errorlist-sqlite3! { nerror -- } \ will add the nerror associated string for that error to database
    5 1 ?do \ try to test and store 5 times after that just bail
	nerror (errorINlist?)
	false =
	if false =
	    if nerror (puterrorINlist) then
	    leave
	else drop i 20 * ms
	then
    loop ;

struct
    cell% field next-node \ 0 indicates no more nodes
    cell% field data-id$
    cell% field data-type$
end-struct data-node%

struct
    cell% field dt_added$
    cell% field ip$
    cell% field port$
    cell% field method$
    cell% field data_table$
    cell% field read_device$
    cell% field store_data$
    cell% field data-node
end-struct device%

create new-device
device% %size allot new-device device% %size erase
variable parse-junk$

: pxml-sensor_name ( caddr u addr -- ) data_table$ $! ;
: pxml-ip          ( caddr u addr -- ) ip$ $! ;
: pxml-port        ( caddr u addr -- ) port$ $! ;
: pxml-method      ( caddr u addr -- ) method$ $! ;
: make-data-node   ( caddr -- ) data-node% %allot dup data-node% %size erase swap ! ;
: pxml-data_name   ( caddr u addr -- )
    data-node @ 0 =
    if
	new-device data-node make-data-node \ make and store first node address at data-node
	new-device data-node @
    else
	new-device data-node @
	begin
	    dup next-node @ dup if swap drop false else drop true then
	until
	dup next-node make-data-node
	next-node @
    then
    { caddr u addr } caddr u 
    34 $split to u to caddr addr data-id$ $! \ need to break up the data_type from data_name in string
    caddr u s\" data_type=\"" search
    if
	11 - swap 11 + swap addr data-type$ $!
    else
	parse-new-er throw
    then ;

: [parse-new-device-xml] ( caddr u -- )
    s" register>" search true =
    if
	2drop \ done parsing this string
    else
	s" register " search true =
	if
	    9 - swap 9 + swap 
	    61 $split 3 - swap 1 + swap 2swap s" pxml-" parse-junk$ $! parse-junk$ $+! parse-junk$ $@ find-name name>int new-device swap execute
	else
	    0 =
	    if
		drop \ there is a empty string on the first $iter use that should be discarded
	    else
		parse-new-er throw \ if that string is not empty then it is an xml error
	    then
	then
    then ;

: parse-new-device-xml ( caddr u -- nflag ) \ string is the xml to register this sensor. nflag is false when data parsed correctly and in structure
    try
	dup 0 =
	if
	    parse-new-er throw
	else
	    temp$ $!
	    temp$ 60 ['] [parse-new-device-xml] $iter
	    datetime$ 1 - new-device dt_added$ $!
	    s" yes" new-device store_data$ $!
	    s" yes" new-device read_device$ $!
	    false 
	then dup if swap drop swap drop then 
    restore
    endtry ;

: view-new-device-data
    new-device dt_added$ $@ type cr
    new-device ip$ $@ type cr
    new-device port$ $@ type cr
    new-device method$ $@ type cr
    new-device data_table$ $@ type cr
    new-device read_device$ $@ type cr
    new-device store_data$ $@ type cr
    new-device data-node @ . cr ;
: view-new-data-node dup data-id$ $@ type dup s"  " type data-type$ $@ type next-node @ .s ;

variable makedn$
: make-data-node-string ( -- cadrr u )
    new-device data-node @ dup 0 <>
    if
	makedn$ $off makedn$ init$ 
	begin
	    dup data-id$ $@ makedn$ $+! s"  " makedn$ $+! dup data-type$ $@ makedn$ $+! s" ," makedn$ $+!
	    next-node @ dup 0 =
	until
	drop makedn$ $@
    else
	drop no-data-node-er throw
    then ;

: create-datalogging-table ( -- nflag ) \ nflag will be false if no errors
    try
	setupsqlite3
	new-device data_table$ $@len 0 = if dltable-name-er throw then
	new-device data_table$ $@ sqlite-table? false <> if table-present-er throw then
	new-device data-node @ 0 = if dltable-name-er throw then
	s" CREATE TABLE " temp$ $!
	new-device data_table$ $@ temp$ $+!
	s" (row INTEGER PRIMARY KEY AUTOINCREMENT,dtime INTEGER," temp$ $+!
	make-data-node-string 1 - temp$ $+! \ remove the , from the string
	s" );" temp$ $+! temp$ $@ dbcmds
	sendsqlite3cmd dberrorthrow
	false
    restore 
    endtry ;

: create-device-entry ( -- )
    setupsqlite3
    s" insert into devices values(NULL," temp$ $! datetime$ temp$ $+!
    s" '" temp$ $+! new-device ip$ $@ temp$ $+! s" ','" temp$ $+!
    new-device port$ $@ temp$ $+! s" ','" temp$ $+!
    new-device method$ $@ temp$ $+! s" ','" temp$ $+!
    new-device data_table$ $@ temp$ $+! s" ','" temp$ $+!
    new-device read_device$ $@ temp$ $+! s" ','" temp$ $+!
    new-device store_data$ $@ temp$ $+! s" ');" temp$ $+!
    temp$ $@ dbcmds sendsqlite3cmd dberrorthrow ;

: rm-datatable? ( -- ) \ will determine if a data table was created in database.  If it was it will try to drop the table.
    try 
	new-device data_table$ $@ dup 0 <>
	if
	    sqlite-table? true =
	    if
		\ remove found table as it is not needed due to registartion error
		setupsqlite3
		s" drop table " temp$ $! new-device data_table$ $@ temp$ $+! s" ;" temp$ $+! temp$ $@ dbcmds
		sendsqlite3cmd dberrorthrow  \ note if this throws then the table may still be there after all 
	    then
	then
	false
    restore drop 
    endtry ;

: register-device ( caddr u -- nflag ) \ will register a new device into database device table if there are no conflics
    try  \ nflag will be false if new device registered and is now in database to be used
	new-device device% %size erase  \ note every time this code runs to parse a new device there will be small memory leak
	new-device dt_added$ init$
	new-device ip$ init$
	new-device port$ init$
	new-device method$ init$
	new-device data_table$ init$
	create-device-table
	create-error-tables
	parse-new-device-xml throw
	new-device ip$ $@ sqlite-ip? throw 
	create-datalogging-table throw
	create-device-entry
	\ possibly check the table for the ip address entered and see if data-table named for ip is also a named table
	false
    restore dup
	if
	    swap drop swap drop \ clean up after error
	  \  dup table-present-er <>
	    \ if \ only delete table if it was not present before trying to create a new one
	\	 rm-datatable?  
	 \   then
	then
    endtry ;

\ make a word to have a local version of the device table and update that table when register-device is used and system restarts
\ need a word to store data in the database for a given device from the device table
\ need a word to retreve the device table info to query the device for data to store in the database!
\ need to write the xml stuff on mbed sensor to register then write code to register via wget with ip and port only so mbed returns registry xlm info
\ write words to take xml for datalogging from sensor then log it into database
\ add the xml output for data from the mbed sensor


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
s" Registration string empty in parse-new-device!"                               exception constant parse-new-er

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

: sqlite-ip-port? ( caddrip u caddrport u -- nflag ) \ search device table for the ip addr and port in the string.  nflag is false if no ip addr and port registered yet
    
;


: create-device-table ( -- ) \ This creates the main table used in the database. This table is used to reference all other tables and then data in database.
    \ table is called devices
    \ row is primary key and is autoincrmented
    \ dt_added is the date time stamp device was added
    \ ip address of the device
    \ port number of the device
    \ method would contain the string to send socket device to get data from device eg "val"
    \ parse_char is the string that separates data from each other ex ","
    \ data_table is the name of the table that contains the logged data for this registered device
    \ read_device is yes or no.  no means device is not read anymore. yes means device is read from still.
    \ store_data is yes or no. no means data_list_id table is not to be writen to anymore. yes means data_list_id table is to be writen to still when device is read from.
    setupsqlite3
    s" CREATE TABLE IF NOT EXISTS devices(row INTEGER PRIMARY KEY AUTOINCREMENT,dt_added INTEGER," temp$ $!
    s" ip TEXT,port TEXT,method TEXT,parse_char TEXT,data_table TEXT," temp$ $+!
    s" read_device TEXT,store_data TEXT );" temp$ $+! temp$ $@
    dbcmds
    sendsqlite3cmd dberrorthrow ;

: init$ ( addr -- ) >r r@ off s" " r> $! ;

struct
    cell% field next-node \ 0 indicates no more nodes
    cell% field data-id$
end-struct data-node%

struct
    cell% field dt_added$
    cell% field ip$
    cell% field port$
    cell% field method$
    cell% field parse_char$
    cell% field data_table$
    cell% field read_device$
    cell% field store_data$
    cell% field data-node
end-struct device%

create new-device
device% %size allot new-device device% %size erase
variable parse-junk$

: view-new-device-data
    new-device dt_added$ $@ type cr
    new-device ip$ $@ type cr
    new-device port$ $@ type cr
    new-device method$ $@ type cr
    new-device parse_char$ $@ type cr
    new-device data_table$ $@ type cr
    new-device read_device$ $@ type cr
    new-device store_data$ $@ type cr
    new-device data-node @ . cr ;
: view-new-data-node dup data-id$ $@ type next-node @ .s ;

: parse-ip          ( caddr u addr -- ) ip$ $! ;
: parse-port        ( caddr u addr -- ) port$ $! ;
: parse-method      ( caddr u addr -- ) method$ $! ;
: parse-parse_char  ( caddr u addr -- ) parse_char$ $! ;
: parse-data_table  ( caddr u addr -- ) data_table$ $! ;
: make-data-node    ( caddr -- ) data-node% %allot dup data-node% %size erase swap ! ;
: parse-data_id     ( caddr u addr -- )
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
    data-id$ $! ;

: [parse-new-device] { caddr u -- }
    caddr u
    s" =" search true =
    if
	dup caddr u rot - 2swap 1 /string 2swap new-device -rot s" parse-" parse-junk$ $! parse-junk$ $+! parse-junk$ $@ find-name name>int execute
    else
	parse-new-er throw
    then ;

: parse-new-device ( caddr u -- nflag ) \ nflag is false when parsing had no errors.  
    try
	dup 0 =
	if
	    parse-new-er throw
	else
	    temp$ $!
	    new-device device% %size erase  \ note every time a this code runs to parse a new device there will be small memory leak
	    new-device dt_added$ init$
	    new-device ip$ init$
	    new-device port$ init$
	    new-device method$ init$
	    new-device parse_char$ init$
	    new-device data_table$ init$
	    temp$ 38 ['] [parse-new-device] $iter
	    datetime$ 1 - new-device dt_added$ $!
	    s" yes" new-device store_data$ $!
	    s" yes" new-device read_device$ $!
	then
	false
    restore dup if swap drop swap drop then 
    endtry ;

variable makedn$ 
: make-data-node-string ( -- caddr u )
    new-device data-node @ dup 0 <>
    if
	makedn$ init$ 
	begin
	    dup data-id$ $@ makedn$ $+! s"  INTEGER," makedn$ $+! 
	    next-node @ dup 0 = 
	until
	drop makedn$ $@ 
    else
	drop s" " 
    then ;

: create-datalogging-table ( -- nflag ) \ nflag will be false if no errors
    try
	setupsqlite3
	new-device data_table$ $@len 0 = if dltable-name-er throw then
	new-device data_table$ $@ sqlite-table? false <> if dltable-name-er throw then
	new-device data-node @ 0 = if dltable-name-er throw then
	s" CREATE TABLE " temp$ $!
	new-device data_table$ $@ temp$ $+!
	s" (row INTEGER PRIMARY KEY AUTOINCREMENT,dtime INTEGER," temp$ $+!
	make-data-node-string 1 - temp$ $+! \ remove the , from the string
	s" );" temp$ $+! temp$ $@ dbcmds
	sendsqlite3cmd dberrorthrow
    restore 
    endtry ;

: create-device-entry ( -- nflag )
;

: register-device ( caddr u -- ) \ will register a new device into database device table if there are no conflics
    parse-new-device throw
    \ check if ip address and port is already used here
    create-datalogging-table throw
    create-device-entry throw ;


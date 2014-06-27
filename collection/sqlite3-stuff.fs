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

100 constant sqlite3-resend-time    \ system will wait for this time in ms if a locked database is found befor resending cmds

path$ $@ db-path$ $! s" /collection/datalogged.data" db-path$ $+!  \ this is the name of the database

\ These are the enumerated errors this code can produce
next-exception @ constant sqlite-errorListStart  \ this is start of enumeration of errors for this code

s" table-id or data-id not present in create-data-list-table!"                   exception constant cdlt-er

next-exception @ constant sqlite-errorListEnd    \ this is end of enumeration of erros for this code


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
	dup throw
    endcase ;

: create-device-table ( -- ) \ used to create a device that is logged
    \ table is called devices
    \ row is primary key and is autoincrmented
    \ dt_added is the date time stamp device was added
    \ ip address of the device
    \ port number of the device
    \ method would contain the string to send socket device to get data from device eg "val"
    \ parse_char is the string that separates data from each other ex ","
    \ data_table is the name of the table that contains the logged data for this registered device
    \ read_device is 0 or 1.  0 means device is not read anymore. 1 means device is read from still
    \ store_data is 0 or 1. 0 means data_list_id table is not to be writen to anymore. 1 means data_list_id table is to be writen to still when device is read from
    setupsqlite3
    s" CREATE TABLE IF NOT EXISTS devices(row INTEGER PRIMARY KEY AUTOINCREMENT,dt_added INTEGER," temp$ $!
    s" ip TEXT,port TEXT,method TEXT,parse_char TEXT,data_table TEXT," temp$ $+!
    s" read_device INTEGER,store_data INTEGER );" temp$ $+!
    dbcmds
    sendsqlite3cmd dberrorthrow ;

variable data-table-id$ data-table-id$ off s" " data-table-id$ $!
variable data-id$       data-id$       off s" " data-id$       $!
variable data-junk$     data-junk$     off s" " data-junk$     $!

: [create-data-id] ( caddr u -- ) \ called by create-data-list-table only
    58 $split 
    58 $split 2drop 2swap 2dup data-id$ $!
    temp$ $+! s"  " temp$ $+! temp$ $+! s" ," temp$ $+! ;

: [create-table-id] ( caddr u -- ) \ called by create-data-list-table only
    58 $split 2drop 2dup data-table-id$ $! 
    temp$ $+! s" (row INTEGER PRIMARY KEY AUTOINCREMENT,dtime INTEGER," temp$ $+! ;

: [create-data-list-table] ( caddr u -- ) \ called by create-data-list-table only
    58 $split 2swap 
    s" table-id" search true =
    if
	2drop [create-table-id] 
    else
	s" data-id" search true =
	if
	    2drop [create-data-id]
	else
	    cdlt-er throw \ did not find table or data id
	then
    then ;

: create-data-list-table ( caddr u -- cdata_table u nflag ) \ takes a string containing table id and data id's
    \ parses these id's and creates a table in the database then returns the table id as a string on the stack
    \ If no table id is found or data id's are not present the table will not be created in database
    \ If no table id is found or data id's then the string returned is undefined and nflag is not false
    \ nflag is false if table id and data id's are found and table created in database
    \ Note this table in the database must be created before the data_table can be used in create-device-table word
    \ eg string for this code could be as follows:
    \ "table-id:mbed1: data-id:temperature:int: data-id:humidity:int: "
    \ table-id: must be the first itme in the string.
    \ there can be many data-id: 's  and they contain an id or sensor value name and the type of data stored in database.
    \ At this time the only type of data that is working is int or INTEGER
    \ Each entry needs to have white space around the entry but first and last entry do not need this.
    \ Each entry needs the elements in the entry separated by : with no spaces in the entry itself
    try
	dup 0 =
	if
	    cdlt-er throw
	else
	    data-junk$ $!
	    setupsqlite3
	    data-table-id$ off s" " data-table-id$ $!
	    data-id$ off s" " data-id$ $!
	    s" CREATE TABLE IF NOT EXISTS " temp$ $!
	    data-junk$ 32 ['] [create-data-list-table] $iter
	    temp$ temp$ $@len 1 - 1 $del s" );" temp$ $+! 
	    temp$ $@ dbcmds
	    sendsqlite3cmd dberrorthrow
	    data-table-id$ $@len 0 =
	    if
		cdlt-er throw
	    else
		data-id$ $@len 0 =
		if
		    cdlt-er throw
		else
		    data-table-id$ $@
		    false
		then
	    then
	then
    restore  
    endtry ;



    
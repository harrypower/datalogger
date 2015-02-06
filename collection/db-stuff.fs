\    Copyright (C) 2015  Philip K. Smith

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

require stringobj.fs
require string.fs
require ../Gforth-Tools/sqlite3_gforth_lib.fs
require gforth-misc-tools.fs

decimal

string heap-new constant temp$      \ a temperatry string 
string heap-new constant buffer$    \ a buffer string
string heap-new constant db-path$   \ db path and name
100 constant sqlite3-resend-time    \ this codes sqlite3 routines will wait for this time in ms if a locked database is found bef resending cmds

path$ @$ db-path$ !$ s" /collection/datalogged.data" db-path$ !+$  \ this is the name of the database

\ These are the enumerated errors this code can produce
next-exception @ constant sqlite-errorListStart  \ this is start of enumeration of errors for this code
s" Table name already present in database file! (change name of table)"          exception constant table-present-er

next-exception @ constant sqlite-errorListEnd    \ this is end of enumeration of errors for this code

: setupsqlite3 ( -- ) \ sets default stuff up for sqlite3 work
    initsqlall
    db-path$ @$ dbname ;

: dberrorthrow ( nerror -- ) \ locked database type errors and buffer overflow error will cause a wait and a resend
    \ if a resend sqlite3 cmd fails then that error is thrown
    \ all other errors will throw that are behond the scope of recovery in this code!
    case
	5     of sqlite3-resend-time ms sendsqlite3cmd throw  endof \ database is locked now
	6     of sqlite3-resend-time ms sendsqlite3cmd throw  endof \ table is locked now
	sqlerrors retbuffover-err @
	of sqlmessg retbuffmaxsize-cell @ 2 * mkretbuff sendsqlite3cmd throw  endof \ buffer overflow from sqlite3
	\ might want to limit this buffer resize to 10k bytes or something like that
	dup throw
    endcase ;

2157 constant db-ok
2156 constant db-errors
: sqlite-integrity-check ( -- nflag ) \ This will do a pragma integrity_check and test returned result
    \ nflag is db-ok if that is the returned result from sqlite3
    \ nflag is db-errors is returned if ok was not returned from sqlite3
    \ nflag can also return other system or sqlite3 messages
    try
	setupsqlite3
	s" pragma integrity_check;" dbcmds
	sendsqlite3cmd dberrorthrow
	dbret$ s" ok" search -rot 2drop true = if db-ok throw then
	dbret$ s" ok" search -rot 2drop false = if db-errors throw then
    restore
    endtry ;

2159 constant table-yes
2158 constant table-no
: sqlite-table? ( caddr u -- nflag ) \ will search db for the string as a table name.
    \ nflag is table-yes (2159) meaning the table is in database
    \ nflag is table-no  (2158) meaning the table is not in database
    \ nflag is some error either  +1 to +110 for some sqlite3 error
    \ nflag is some error either -1 to -x for some system error or other returned error defined with exception
    try
	buffer$ !$
	setupsqlite3
	s" " dbfieldseparator
	s" " dbrecordseparator
	s" select name from sqlite_master where name = '" temp$ !$ buffer$ @$ temp$ !+$ s" ';" temp$ !+$ temp$ @$ dbcmds
	sendsqlite3cmd dberrorthrow
	dbret$ buffer$ @$ search -rot 2drop true = if table-yes throw else table-no throw then
    restore swap drop swap drop
    endtry ;

: create-error-tables ( -- ) \ used to create the error logging tables
    setupsqlite3
    s" CREATE TABLE IF NOT EXISTS errors(row INTEGER PRIMARY KEY AUTOINCREMENT,dtime INTEGER,error INT);" dbcmds
    sendsqlite3cmd dberrorthrow
    setupsqlite3
    s" CREATE TABLE IF NOT EXISTS errorList(error INT UNIQUE,errorText TEXT);" dbcmds
    sendsqlite3cmd dberrorthrow ;

: error-sqlite3! ( nerror -- ) \ used to store error values into errors table
    try
	setupsqlite3
	s" insert into errors values(NULL," temp$ $!
	datetime$ temp$ !+$
	#to$ temp$ !+$
	s" );" temp$ !+$
	temp$ @$ dbcmds
	sendsqlite3cmd  dberrorthrow
	false
    restore if drop then \ note this word will not pass any errors upwards so nothing is returned !
    endtry ;

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
next-exception @ value sqlite-errorListStart  \ this allows enumeration of errors for this code

s" stub message"                            exception constant stubb-er

next-exception @ value sqlite-errorListEnd

: setupsqlite3 ( -- ) \ sets default stuff up for sqlite3 work
    initsqlall
    db-path$ $@ dbname ;

: dberrorthrow ( nerror -- nflag ) \ locked database type errors and buffer overflow error will cause a wait and a resend
    \ if a resend sqlite3 cmd fails then that error is thrown
    \ nflag returns false if nerror is false because this means no errors
    \ all other errors will throw that are behond the scope of recovery in this code!
    case
	5     of sqlite3-resend-time ms sendsqlite3cmd dup throw  endof \ database is locked now
	6     of sqlite3-resend-time ms sendsqlite3cmd dup throw  endof \ table is locked now
	sqlerrors retbuffover-err @
	      of sqlmessg retbuffmaxsize-cell @ 2 * mkretbuff sendsqlite3cmd dup throw  endof \ buffer overflow from sqlite3
	dup throw
    endcase ;

: create-device-table ( -- )
    setupsqlite3
    s" CREATE TABLE IF NOT EXISTS devices(row INT PRIMARY KEY AUTOINCREMENT,dt_added INT,ip TEXT,port TEXT,method TEXT,data_list_id TEXT);"
    dbcmds
    sendsqlite3cmd dberrorthrow ;


    
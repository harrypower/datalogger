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

warnings off

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
s" ErrorList query for an error did not return expected number!"                 exception constant errorlist-number-err
s" Error place holder #1"                                                        exception constant errorholder1
s" Error place holder #2"                                                        exception constant errorholder2
next-exception @ constant sqlite-errorListEnd    \ this is end of enumeration of errors for this code

: error#to$ ( nerror -- caddr u )  \ takes an nerror number and gives the string for that error
    >r sqlmessg error-cell @ 0 <> r@ 1 >= r@ 101 <= and and r> swap
    if
	dberrmsg drop \ test for a sqlite3 error and return sqlite3 string if the error is from sqlite3
    else
	>stderr Errlink   \ tested with gforth ver 0.7.0 and 0.7.3
	begin             \ if the nerror does not exist then a null string is returned!
	    @ dup
	while
		2dup cell+ @ =
		if
		    2 cells + count rot drop exit
		then
	repeat
    then ;

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
    s" CREATE TABLE IF NOT EXISTS errors(row INTEGER PRIMARY KEY AUTOINCREMENT,dtime INTEGER,error INT,errorSent INT);" dbcmds
    sendsqlite3cmd dberrorthrow
    setupsqlite3
    s" CREATE TABLE IF NOT EXISTS errorList(error INT UNIQUE,errorText TEXT);" dbcmds
    sendsqlite3cmd dberrorthrow ;

: error-sqlite3! ( nerror -- ) \ used to store error values into errors table
    try
	setupsqlite3
	s" insert into errors values(NULL," temp$ !$
	datetime$ temp$ !+$   \ dtime
	#to$ temp$ !+$        \ error
	s" ,0 );" temp$ !+$   \ errorSent ( false for not sent and true for sent )
	temp$ @$ dbcmds
	sendsqlite3cmd  dberrorthrow
	false
    restore if drop then \ note this word will not pass any errors upwards so nothing is returned !
    endtry ;

2164 constant error-yes
2165 constant error-no
: (errorINlist?) { nsyserror -- nflag }  \ see if nsyserror number is in database list of errors
    \ nflag is error-yes if nsyserror is in the dbase
    \ nflag is error-no if nsyserror is not in the dbase
    \ nflag can be other error numbers
    try
	setupsqlite3
	s" " dbfieldseparator
	s" " dbrecordseparator
	s" select error from errorList where (error = " temp$ !$
	nsyserror #to$ temp$ !+$
	s" );" temp$ !+$ temp$ @$ dbcmds
	sendsqlite3cmd dberrorthrow
	dbret$ dup 0 =
	if
	    \ the error is not in the list at this time
	    2drop error-no throw
	else
	    s>number? false =
	    if
		\ number not recieved from errorList query
		2drop errorlist-number-err throw
	    else
		d>s nsyserror <>
		if
		    errorlist-number-err throw
		else
		    error-yes throw
		then
	    then
	then
    restore
    endtry ;

: (puterrorINlist) ( nerror -- ) \ add nerror number and string to the error list
    setupsqlite3
    dup -2 <>  \ this is needed because -2 error does not report a message even though it is Abort !
    if
s" insert into errorList values(" temp$ !$
	dup  #to$, temp$ !+$ s\" \'" temp$ !+$
	error#to$ temp$ !+$ s\" \');" temp$ !+$ temp$ @$ dbcmds
    else
	drop
	s\" insert into errorList values(-2, \'Abort has occured!\');" dbcmds
    then
    sendsqlite3cmd drop \ ****note if there is a sqlite3 error here this new error will not be stored in list*****
    \ **** positive error numbers can come from sqlite3 but the proper string may not get placed in db for the number
;

: errorlist-sqlite3! ( nerror -- ) \ will add the nerror associated string for that error to database
    0 { nerror ntest -- }
    try
	5 0 ?do \ try to test and store 5 times after that just bail
	    nerror (errorINlist?) to ntest ntest error-no =
	    if
		nerror (puterrorINlist) leave
	    then
	    ntest error-yes <>
	    if
		i 20 * ms
	    else
		leave
	    then
	loop
	false
    restore drop \ nothing returned by this word even if an error happens in this word!
    endtry ;

: error! ( nerror -- ) \ store nerror in database and string for error message
    dup error-sqlite3!
    errorlist-sqlite3! ;

string heap-new constant junky$
: lastlocalerror$@ ( -- ncaddr-error uerror ) \ retreave the last error full string with #'s
    setupsqlite3
    s" " sqlmessg fseparator-$ z$!
    s" " sqlmessg rseparator-$ z$!
    s" select error from errors limit 1 offset ((select max(row) from errors)-1);" temp$ !$
    temp$ @$ dbcmds sendsqlite3cmd dberrorthrow dbret$
    s" select errorText from errorList where error = " temp$ !$ temp$ !+$
    temp$ @$ dbcmds sendsqlite3cmd dberrorthrow dbret$ junky$ !$
    s" ," sqlmessg fseparator-$ z$!
    s" select row,datetime(dtime,'unixepoch','localtime'),error,errorSent " temp$ !$
    s" from errors limit 1 offset ((select max(row) from errors)-1);" temp$ !+$
    temp$ @$ dbcmds sendsqlite3cmd dberrorthrow dbret$ temp$ !$ junky$ @$ temp$ !+$ temp$ @$ ;

: lastlocalerror#@ ( -- ncaddr-error uerror ) \ retreave the last error stored in errors table
    setupsqlite3
    s" select row,datetime(dtime,'unixepoch','localtime'),error,errorSent " temp$ !$
    s" from errors limit 1 offset ((select max(row) from errors)-1);" temp$ !+$
    temp$ @$ dbcmds sendsqlite3cmd dberrorthrow dbret$ ;

: lastlocalerror#>$@ ( nerrorID -- ncaddr-error uerror ) \ retreave the error string from nerrorID
    setupsqlite3
    s" select error,errorText from errorList where error = " temp$ !$
    #to$ temp$ !+$ s" ;" temp$ !+$
    temp$ @$ dbcmds sendsqlite3cmd dberrorthrow dbret$ ;

: create-localdata ( -- ) \ create the local table of data from sensors
    setupsqlite3
    s" CREATE TABLE IF NoT EXISTS localData(row INTEGER PRIMARY KEY AUTOINCREMENT,dtime INT," temp$ !$
    s" temp REAL,humd REAL,pressure INT,co2 REAL,nh3 REAL,dataSent INT );" temp$ !+$
    temp$ @$ dbcmds sendsqlite3cmd dberrorthrow ;

: localdata! ( ntime npress -- ) ( F: ftemp fhumd fco2 fnh3 -- ) \ store local data!
    { ntime F: ftemp F: fhumd npres F: fco2 F: fnh3 -- } \ store data into localData table of DB
    setupsqlite3
    s" insert into localData values(NULL," temp$ !$
    ntime #to$, temp$ !+$   \ remember ntime is a one cell size
    ftemp fto$, temp$ !+$
    fhumd fto$, temp$ !+$
    npres #to$, temp$ !+$
    fco2 fto$, temp$ !+$
    fnh3 fto$, temp$ !+$
    s" 0);" temp$ !+$       \ dataSent can be 0 for not sent or -1 for sent
    temp$ @$ dbcmds sendsqlite3cmd dberrorthrow ;

: lastlocaldata@ ( -- ncaddr u ) \ simply output a string of the last data point stored in localData table
    setupsqlite3
    s" select row,datetime(dtime,'unixepoch','localtime'),temp,humd,pressure,co2,nh3,dataSent " temp$ !$
    s" from localData limit 1 offset ((select max(row) from localData)-1);" temp$ !+$
    temp$ @$ dbcmds sendsqlite3cmd dberrorthrow dbret$ ;

: nlastlocaldata@ ( uqty -- ncaddr u ) \ retrieve nqty rows from local database taking rows from last row first
    setupsqlite3
    dup 100 * mkretbuff \ uqty * 100 = amount to change return buffer size to
    s" select datetime(dtime,'unixepoch','localtime'),temp,humd,pressure,co2,nh3 " temp$ !$
    s" from localData limit " temp$ !+$
    #to$ 2dup temp$ !+$
    s"  offset ((select max(row) from localData)-" temp$ !+$
    temp$ !+$
    s" );" temp$ !+$
    temp$ @$ dbcmds sendsqlite3cmd dberrorthrow dbret$ ;

: create-remotedata ( -- ) \ create the remote table of data for remote sensor storage
    setupsqlite3
    s" CREATE TABLE IF NOT EXISTS remoteData(row INTEGER PRIMARY KEY AUTOINCREMENT,dtime INT," temp$ !$
    s" temp REAL,humd REAL,pressure INT,co2 REAL,nh3 REAL,deviceID TEXT,receivedTime INT," temp$ !+$
    s" remoteRow INT);" temp$ !+$
    temp$ @$ dbcmds sendsqlite3cmd dberrorthrow ;

: create-remoterror ( -- ) \ create the remote table of errors for remote error data
    setupsqlite3
    s" CREATE TABLE IF NOT EXISTS remoteErrors(row INTEGER PRIMARY KEY AUTOINCREMENT,dtime INT," temp$ !$
    s" error INT,errorText TEXT,deviceID TEXT,receivedTime INT,remoteRow INT);" temp$ !+$
    temp$ @$ dbcmds sendsqlite3cmd dberrorthrow ;

: remotedata! ( ntime npres ncaddr-id uid nrtime -- ) ( f: ftemp fhumd fco2 fnh3 -- )
    { ntime F: ftemp F: fhumd npres F: fco2 F: fnh3 ncaddr-id uid nrtime nrrow }
    setupsqlite3
    s" insert into remoteData values(NULL," temp$ !$
    ntime #to$, temp$ !+$   \ remember ntime is a one cell size
    ftemp fto$, temp$ !+$
    fhumd fto$, temp$ !+$
    npres #to$, temp$ !+$
    fco2  fto$, temp$ !+$
    fnh3  fto$, temp$ !+$
    ncaddr-id uid $>wrapped$ temp$ !+$ s" ," temp$ !+$
    nrtime #to$, temp$ !+$
    nrrow  #to$  temp$ !+$
    s" );" temp$ !+$
    temp$ @$ dbcmds sendsqlite3cmd dberrorthrow ;

: lastremotedata@ ( -- ncaddr u ) \ output string of last data point stored in remotedata table
    setupsqlite3
    s" select row,datetime(dtime,'unixepoch','localtime'),temp,humd,pressure,co2,nh3," temp$ !$
    s" deviceID,datetime(receivedTime,'unixepoch','localtime'),remoteRow " temp$ !+$
    s" from remoteData limit 1 offset ((select max(row) from remoteData)-1);" temp$ !+$
    temp$ @$ dbcmds sendsqlite3cmd dberrorthrow dbret$ ;

: remoterror!  { ntime nerror ncaddrerror uerror ncaddrid uid nrtime nrrow -- }
    \ store the remote error data into remoteError table
    setupsqlite3
    s" insert into remoteErrors values(NULL," temp$ !$
    ntime   #to$, temp$ !+$
    nerror  #to$, temp$ !+$
    ncaddrerror uerror $>wrapped$ temp$ !+$ s" ," temp$ !+$
    ncaddrid uid $>wrapped$ temp$ !+$ s" ," temp$ !+$
    nrtime  #to$, temp$ !+$
    nrrow   #to$ temp$ !+$
    s" );" temp$ !+$
    temp$ @$ dbcmds sendsqlite3cmd dberrorthrow ;

: lastremoterror@ ( -- ncaddr u ) \ output string of last remote error in remoteErrors table
    setupsqlite3
    s" select row,datetime(dtime,'unixepoch','localtime'),error,errorText," temp$ !$
    s" deviceID,datetime(receivedTime,'unixepoch','localtime'),remoteRow " temp$ !+$
    s" from remoteErrors limit 1 offset ((select max(row) from remoteErrors)-1);" temp$ !+$
    temp$ @$ dbcmds sendsqlite3cmd dberrorthrow dbret$ ;

: makedb ( -- ) \ will create the db file and set up the tables if this file does not exist already
    db-path$ @$ filetest false =
    if \ there is no db file now
	db-path$ @$ w/o create-file throw close-file throw
    then
    create-error-tables
    create-localdata
    create-remotedata
    create-remoterror ;

: getlocalnonsent ( -- caddr u ) \ gets first non sent local data set in a string format
    setupsqlite3
    s" " dbrecordseparator
    s" select row,datetime(dtime,'unixepoch','localtime'),temp,humd," temp$ !$
    s" pressure,co2,nh3 from localData where (dataSent is 0) limit 1;" temp$ !+$
    temp$ @$ dbcmds sendsqlite3cmd dberrorthrow dbret$ ;

: setlocalrowsent ( nrow -- ) \ will set the dataSent flag to sent of nrow
    setupsqlite3
    s" update localData set dataSent = '-1' where row = '" temp$ !$
    #to$ temp$ !+$ s" ';" temp$ !+$
    temp$ @$ dbcmds sendsqlite3cmd dberrorthrow ;

: clearlocalrowsent ( nrow -- ) \ will clear the dataSent flag of nrow
    setupsqlite3
    s" update localData set dataSent = '0' where row = '" temp$ !$
    #to$ temp$ !+$ s" ';" temp$ !+$
    temp$ @$ dbcmds sendsqlite3cmd dberrorthrow ;


: getlocalRownonsent ( nrow -- caddr u ) \ will return data of nrow that meets requirement of non sent
    setupsqlite3
    s" " dbrecordseparator
    s" select row,dtime,temp,humd," temp$ !$
    s" pressure,co2,nh3 from localData where ((dataSent is 0) and (row is " temp$ !+$
    #to$ temp$ !+$ s" ));" temp$ !+$
    temp$ @$ dbcmds sendsqlite3cmd dberrorthrow dbret$ ;

: getlocalrow#nonsent ( -- nrow ) \ will return nrow of first non sent row number in database
    setupsqlite3                 \ will throw for errors
    s" " dbrecordseparator       \ if nrow is 0 then there are no rows in database to sent
    s" " dbfieldseparator
    s" select row from localData where dataSent is 0 limit 1;" temp$ !$
    temp$ @$ dbcmds sendsqlite3cmd dberrorthrow dbret$
    s>number? true = if d>s else -1 throw then ;

: getlocalerroRownonsent ( nrow -- caddr u )
    setupsqlite3
    s" " dbrecordseparator
    s" select row,dtime,error,( select errorText from errorList where ((select error from errorList) is " temp$ !$
    s" (select error from errors))) from errors where row is " temp$ !+$
    #to$ temp$ !+$ s" ;" temp$ !+$
    temp$ @$ dbcmds sendsqlite3cmd dberrorthrow dbret$ ;

: getlocalerrorow#nonsent ( -- nrow )
    setupsqlite3
    s" " dbrecordseparator
    s" " dbfieldseparator
    s" select row from errors where errorSent is 0 limit 1;" temp$ !$
    temp$ @$ dbcmds sendsqlite3cmd dberrorthrow dbret$
    s>number? true = if d>s else -1 throw then ;

: setlocalerrorowsent ( nrow -- )
    setupsqlite3
    s" update errors set errorSent = '-1' where row = '" temp$ !$
    #to$ temp$ !+$ s" ';" temp$ !+$
    temp$ @$ dbcmds sendsqlite3cmd dberrorthrow ;

: clearlocalerrorowsent  ( nrow -- )
    setupsqlite3
    s" update errors set errorSent = '0' where row = '" temp$ !$
    #to$ temp$ !+$ s" ';" temp$ !+$
    temp$ @$ dbcmds sendsqlite3cmd dberrorthrow ;

: getlocalNminmaxavg ( setstrings -- outstrings )
  \ setstrings contains the following in order
  \ field name (co2,nh3,temp,humd,presure)
  \ time frame ( H,d)
  \ start year ( 2015 )
  \ start month ( 08 )
  \ start day ( 12 )
  \ start hour ( 00 )
  \ start minute ( 00 )
  \ quantity to view ( 1 )
  setupsqlite3
  s" select strftime"
;

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

\  This code is the server portion that recieves decrypts and stores data into
\  database defined in db-stuff.fs
\  This is normaly executed inside a web page and data is received from post variable.
\  The data is stored as remote data as defined in db-stuff.fs not local data.

\ This code is normaly executed as a script from a web page but for testing purposes
\ the testingflag constant detects if it is run locally
s" testingflag" find-name 0 =
[if]
    false constant testingflag \ false for normal remote use
[then]
testingflag [if]
    require string.fs  
    variable posted 0 posted !
    require cryptobj.fs
    require stringobj.fs
    require db-stuff.fs
[else]
    require ../collection/cryptobj.fs
    require ../collection/stringobj.fs
    require ../collection/db-stuff.fs
[then]

string heap-new constant passphrase$
string heap-new constant ddata$
strings heap-new constant dbdata$
string heap-new constant message$

path$ @$ passphrase$ !$ s" /collection/testpassphrase" passphrase$ !+$

path$ @$ encrypt_decrypt heap-new value edata


: posttest ( -- nflag ) \ nflag is true if there is a post message
    posted @ 0 <> ;     \ nflag is false if there is no post message

: getdecryptpost ( -- nflag ) \ if post data is present decrypt it into ddata$ string
    \ nflag is true if decryption and post test passed
    \ nflag is false if decryption or post test failed
    posttest
    if
        posted $@ passphrase$ @$ edata decrypt$ 0 =
        if ddata$ !$ true
        else
            2drop false
        then
    else
        false
    then ;

: parsemessage ( -- )
    dbdata$ construct
    s" ," ddata$ @$ dbdata$ split$>$s ;

: validdata ( -- nflag ) \ check ddata$ for valid data and put data into dbdata$
    \ nflag is true if the message is data type
    \ nflag is false if the message is not valid for data type
    parsemessage dbdata$ reset dbdata$ @$x
    s" DATA" compare 0 = 
    dbdata$ $qty 9 = and ;
: storedata ( -- nflag ) \ take dbdata$ and store it into database
    \ nflag is true if some parsing or storage error happened
    \ nflag is false if data was parsed and stored into database ok
    try   
	1 dbdata$ []@$ throw s>number?  true <> throw d>s
	2 dbdata$ []@$ throw >float     true <> throw
	3 dbdata$ []@$ throw >float     true <> throw
	4 dbdata$ []@$ throw s>unumber? true <> throw d>s
	5 dbdata$ []@$ throw >float     true <> throw
	6 dbdata$ []@$ throw >float     true <> throw
	7 dbdata$ []@$ throw 
	datetime
	8 dbdata$ []@$ throw s>unumber? true <> throw d>s
	remotedata!
	false
    restore 
    endtry ;

: validerror ( -- nflag ) \ check ddata$ for valid error info and put error into dbdata$
;
: storeerror ( -- nflag ) \ take dbdata$ and store the error info into database
;

: valid-data-error! ( -- nflag ) \ check if valid data or error message and store it
    \ nflag is true if data or error message is valid
    \ nflag is false if something is wrong with data or error message or storing data
    validdata true = 
    storedata false = and
   \ put the validerror and storeerror words here to test if the validdata is false!
;

: validatestore ( -- nflag ) \ test for data or error info and store it
    \ nflag is false if data or error info was stored in database
    \ nflag is true if some failure happened
    try
	message$ construct
	posttest false = if s"  FAIL ERROR:no post message!" message$ !+$ true throw  then
	getdecryptpost
	if
	    valid-data-error!
	    if
		s" PASS" message$ !$ 
	    else
		s" FAIL ERROR:validation test failed!" message$ !$ true throw 
	    then
	else
	    s"  FAIL ERROR:decryption failed!" message$ !+$ true throw
	then
	false
    restore 
    endtry ;

testingflag false = [if]
    validatestore drop message$ @$ type
[then]


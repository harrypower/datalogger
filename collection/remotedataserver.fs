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

\ This code is normaly executed as a script form a web page but for testing purposes
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
: validdata ( -- nflag ) \ check ddata$ for valid data and put data into dbdata$
;
: storedata ( -- nflag ) \ take dbdata$ and store it into database
;

: validerror ( -- nflag ) \ check ddata$ for valid error info and put error into dbdata$
;
: storeerror ( -- nflag ) \ take dbdata$ and store the error info into database
;

: validatestore ( -- nflag ) \ test for data or error info and store it
    \ nflag is false if data or error info was stored in database
    \ nflag is true if some failure happened
    try
	posttest false = if ."  FAIL ERROR:no post message!" true throw  then
	getdecryptpost if ." PASS" else ."  FAIL ERROR:decryption failed!" true throw then
	false
    restore 
    endtry ;

testingflag false = [if]
    validatestore
[then]
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

testingflag false = [if]
    posttest false = [if] ." FAIL no post message!" [then]
    getdecryptpost [if] ." PASS" [else] ." FAIL" [then]
[then]
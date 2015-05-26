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

\ This code simply retrieves sensor data from db-stuff.fs words
\ then encrypts the data to send to the server of this sensor data!

\ warnings off
require cryptobj.fs
require stringobj.fs
require gforth-misc-tools.fs
require db-stuff.fs
require script.fs

decimal

string heap-new constant passf$     \ path & file name to passphrase file
string heap-new constant edata$     \ encrypted data file name and path to send to server
string heap-new constant senddata$  
string heap-new constant identity$
string heap-new constant junk$
strings heap-new constant data$s
0 value myed                        \ contains the encrypt_dectryp object after setup

variable sendingrow#                \ the data or error row that is being sent 

: setup-stuff ( -- ) \ setup strings initalized and encryption object started
\ this passphrase file must exist
\ note the first line is only used in the passphrase file
path$ @$ passf$ !$ s" /collection/testpassphrase" passf$ !+$
passf$ @$ filetest false = if abort" The passphrase file is not present!" then
\ get identity string for use later
path$ @$ junk$ !$ s" /identityinfo.data" junk$ !+$ junk$ @$ slurp-file
s" id:" search true = if 3 - swap 3 + swap identity$ !$ else abort" Identity not present!" then
s" :" identity$ split$ if 2drop identity$ !$ else 2drop 2drop abort" Identity bad format!" then
\ this is the name of the encrypted data file that is used 
path$ @$ junk$ !$ s" /collection/endata.data" junk$ !+$ junk$ @$ edata$ !$
\ this is the encrypt_decrypt object named myed
path$ @$ junk$ !$ s" /collection" junk$ !+$ junk$ @$ encrypt_decrypt heap-new to myed ;
setup-stuff  

\ this next word is needed to prevent system defunct processes when using sh-get from script.fs
string heap-new constant shlast$
: shgets ( caddr u -- caddr1 u1 nflag ) \ like shget but will not produce defunct processes
    TRY   \ nflag is false the addr1 u1 is the result string from the sh command
	\ nflag is true could mean memory was not allocated for this command or sh command failed
	shlast$ !$
	s"  ; echo ' ****'" shlast$ !+$ shlast$ @$ sh-get
	2dup shlast$ !$
	s\"  ****\x0a" search true =
	if
	    swap drop 6 =
	    if
		shlast$ @$ 6 - shlast$ !$ shlast$ @$
	    else
		shlast$ @$
	    then
	else
	    shlast$ @$
	then $?
    RESTORE
    ENDTRY ;

: makesenddata$ ( -- ) \ used to reorder data for sending
    2 data$s []@$ throw senddata$ !$  s" ," senddata$ !+$
    3 data$s []@$ throw senddata$ !+$ s" ," senddata$ !+$
    4 data$s []@$ throw senddata$ !+$ s" ," senddata$ !+$
    5 data$s []@$ throw senddata$ !+$ s" ," senddata$ !+$
    6 data$s []@$ throw senddata$ !+$ s" ," senddata$ !+$
    7 data$s []@$ throw senddata$ !+$ s" ," senddata$ !+$
    0 data$s []@$ throw senddata$ !+$ s" ," senddata$ !+$ ;

: makeencryptdata$ ( -- ) \ encrypted the data for sending
    senddata$ @$ passf$ @$ myed encrypt$ throw 
    senddata$ !$ ;

: getencryptdata ( -- ) \ get, order and encrypt data for sending
    getlocalrow#nonsent dup sendingrow# !
    getlocalRownonsent junk$ !$ identity$ @$ junk$ !+$ s" ," junk$ !+$
    data$s construct
    s" ," junk$ @$ data$s split$>$s
    makesenddata$
    makeencryptdata$ ;

: data>file ( -- ) \ store the encrypted data to a file for later use
    0 { efid }
    edata$ @$ filetest true =
    if
	edata$ @$ delete-file throw
    then
    edata$ @$ w/o bin create-file throw to efid
    senddata$ @$ efid write-file throw
    efid flush-file throw
    efid close-file throw ;

: data>server ( -- ) \ send encrypted data via curl to server
    s" curl --data-binary @" junk$ !$ edata$ @$ junk$ !+$
    s"  192.168.0.113:4445/receivedata.shtml" junk$ !+$
    junk$ @$ shgets dup 0 =
    if
	drop ." message recieved is:" cr type
    else
	. ."  some error in the curl statement occured!"
    then
;

: doencryptsend ( -- )
    getencryptdata
    data>file
    data>server
    edata$ @$ delete-file throw ;

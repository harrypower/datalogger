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
\ This code is executed on a sensor device to upload all the sensors data to server
\ ** The passphrase file and the identityinfo.data file must exist for this code to work! **

true constant testingflag \ true for locally testing and false for normal remote use
testingflag [if]
    require remotedataserver.fs
[then]

warnings off
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
string heap-new constant serveraddr$
string heap-new constant junk$
strings heap-new constant data$s
string heap-new constant servermessage$
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
path$ @$ junk$ !$ s" /serveraddr.data" junk$ !+$ junk$ @$ slurp-file
s\" serveraddr\"" search true = if 11 - swap 11 + swap serveraddr$ !$ else abort" No server address!" then
s\" \"" serveraddr$ split$ if 2drop serveraddr$ !$ else 2drop 2drop abort" Server address bad format!" then 
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

: makesenddata$ ( -- ) \ used to reorder data for sending and add DATA string
    senddata$ construct
    s" DATA," senddata$ !$
    7 data$s []@$ throw senddata$ !+$ s" ," senddata$ !+$
    1 data$s []@$ throw senddata$ !+$ s" ," senddata$ !+$
    2 data$s []@$ throw senddata$ !+$ s" ," senddata$ !+$
    3 data$s []@$ throw senddata$ !+$ s" ," senddata$ !+$
    4 data$s []@$ throw senddata$ !+$ s" ," senddata$ !+$
    5 data$s []@$ throw senddata$ !+$ s" ," senddata$ !+$
    6 data$s []@$ throw senddata$ !+$ s" ," senddata$ !+$
    0 data$s []@$ throw senddata$ !+$ s" ," senddata$ !+$ ;
: makesenderror$ ( -- ) \ reorder data for sending and add ERROR string
    senddata$ construct
    s" ERROR," senddata$ !$
    4 data$s []@$ throw senddata$ !+$ s" ," senddata$ !+$
    1 data$s []@$ throw senddata$ !+$ s" ," senddata$ !+$
    2 data$s []@$ throw senddata$ !+$ s" ," senddata$ !+$
    3 data$s []@$ throw senddata$ !+$ s" ," senddata$ !+$
    0 data$s []@$ throw senddata$ !+$ s" ," senddata$ !+$ ;

: makeencryptdata$ ( -- ) \ encrypted the data for sending
    senddata$ @$ passf$ @$ myed encrypt$ throw 
    senddata$ !$ ;
: makeencrypterror$ ( -- ) \ encrypte the data dor sending 
    makeencryptdata$ ;

: getencryptdata ( -- ) \ get, order and encrypt data for sending
    getlocalrow#nonsent dup sendingrow# !
    getlocalRownonsent junk$ !$ identity$ @$ junk$ !+$ s" ," junk$ !+$
    data$s construct
    s" ," junk$ @$ data$s split$>$s
    makesenddata$
    makeencryptdata$ ;
: getencrypterror ( -- )
    getlocalerrorow#nonsent dup sendingrow# !
    getlocalerroRownonsent junk$ !$ identity$ @$ junk$ !+$ s" ," junk$ !+$
    data$s construct
    s" ," junk$ @$ data$s split$>$s
    makesenderror$
    makeencrypterror$ ;

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

: error>file ( -- )
    data>file ;

testingflag 
[if]
    \ send data via local word execution
    : data>server ( -- ) \ send encrypted data as local test to server code
	edata$ @$ slurp-file posted $! ;
    : error>server ( -- )
	data>server ;
    
    : dataencryptsend ( -- nflag ) \ get data encrypt data send data
	\ nflag is true if all ok nflag is false if some failure happened 
	getencryptdata
	data>file
	data>server
	validatestore drop message$ @$ servermessage$ !$
	servermessage$ @$ s" PASS" search swap drop swap drop 
	edata$ @$ delete-file throw ;
    : errorencryptsend ( -- nflag )
	getencrypterror
	error>file
	error>server
	validatestore drop message$ @$ servermessage$ !$
	servermessage$ @$ s" PASS" search swap drop swap drop
	edata$ @$ delete-file throw ;
    
[else]
    \ send data via tcp
    : data>server ( -- ) \ send encrypted data via curl to server
	s" curl --data-binary @" junk$ !$ edata$ @$ junk$ !+$
	serveraddr$ @$ junk$ !+$
	junk$ @$ shgets dup 0 =
	if
	    drop servermessage$ !$
	else
	    servermessage$ !$
	    s\" \n" servermessage$ !+$
	    s" FAIL ERROR:some curl error occured " servermessage$ !+$
	    #to$ servermessage$ !+$ s" !" servermessage$ !+$
	then ;
    : error>server ( -- )
	data>server ;
    
    : dataencryptsend ( -- nflag )
	getencryptdata
	data>file
	data>server
	servermessage$ @$ s" PASS" search swap drop swap drop true <>
	edata$ @$ delete-file throw ;
    : errorencryptsend ( -- nflag )
	getencrypterror
	error>file
	error>server
	servermessage$ @$ s" PASS" search swap drop swap drop true <>
	edata$ @$ delete-file throw ;
    
[then]


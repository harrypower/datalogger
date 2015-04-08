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
\
\ This code is for string handling.   There are two objects.
\ Object string is for single strings and all memory is managed by object.
\ Object strings uses object string to handle as many stings of any size you want!
\ You can think of string as a container for a string and strings is a container for
\ any collection of strings you want.

require objects.fs

object class
    cell% inst-var string-addr
    cell% inst-var string-size
    cell% inst-var string-test
    m: ( string -- ) \ initalize the string
	string-test @ string-test =
	if string-addr @ free throw then
	0 string-test !
	0 string-addr !
	0 string-size ! ;m overrides construct
    m: ( string -- )     \ free allocated memory for this instance of object
	this construct ;m method string-destruct
    m: ( caddr u string -- ) \ store string
	    dup allocate throw { u caddr1 }
	    caddr1 u move
	    string-test @ string-test =
	    if
		string-addr @ free throw
		0 string-test ! 
	    then
	    caddr1 string-addr !
	    u string-size ! 
	    string-test string-test ! ;m method !$
    m: ( string -- caddr u ) \ retrieve string
	string-test @ string-test =
	if
	    string-addr @ string-size @
	else 0 0
	then ;m method @$
    m: ( caddr u string -- ) \ add a string to this string
	string-test @ string-test =
	if \ resize
	    dup 0 >
	    if
		dup string-size @ + string-addr @ swap resize throw
		dup string-addr ! string-size @ + swap dup string-size @ + string-size ! move
	    else 2drop
	    then
	else
	    dup 0 >
	    if
		dup allocate throw
		dup string-addr !
		swap dup string-size ! move
		string-test string-test !
	    else 2drop
	    then
	then ;m method !+$
    m: ( caddr u string -- caddr1 u1 caddr2 u2 nflag ) \ split the stored string at caddr u if found
	\ caddr u will be removed and caddr1 u1 will be split string before caddr u
	\ caddr2 u2 will be the split string after caddr u
	\ if no match found caddr1 u1 returns and empty string and caddr2 u2 contains the this objects string
	\ nflag is true if string is split and returned false if this objects string is returned 
	\ Note the returned strings are valid until a new string is placed in this string object
	{ caddr u }
	string-addr @ string-size @ caddr u search true =
	if
	    dup string-size @ swap - string-addr @ swap 2swap u - swap u + swap true
	else
	    2drop 0 0 string-addr @ string-size @ false
	then ;m method split$
    
    m: ( string -- u ) \ report string size
	string-test @ string-test =
	if string-size @ else 0 then ;m method len$ 
    m: ( string -- ) \ retrieve string object info
	this [parent] print
	s"  string-test:" type string-test @ string-test = .
	s"  addr:" type string-addr @ .
	s"  size:" type string-size @ .
	s"  string:" type string-addr @ string-size @ type ;m overrides print
end-class string

object class
    cell% inst-var array \ contains first address of allocated string object
    cell% inst-var qty
    cell% inst-var index
    cell% inst-var strings-test
    m: ( strings -- ) \ initalize strings object
	strings-test @ strings-test =
	if
	    array @ 0 >
	    if \ deallocate memory allocated for the array and free the string objects
		qty @ 0 ?do array @ i cell * + @ dup string-destruct free throw loop
		array @ free throw
	    then
	    0 qty !
	    0 index !
	    0 array !
	else
	    0 qty !
	    0 index !
	    0 array !
	    strings-test strings-test !
	then ;m overrides construct
    m: ( strings -- ) \ deallocate this object and other allocated memory in this object
	this construct ;m method strings-destruct
    m: ( caddr u strings -- ) \ store a string in handler
	array @ 0 =
	if
	    cell allocate throw array !
	    1 qty !
	    string heap-new dup array @ ! !$
	else
	    array @ cell qty @ 1 + * resize throw dup array !
	    cell qty @ * + dup 
	    string heap-new swap ! @ !$
	    qty @ 1+ qty !
	then 0 index ! ;m method !$x
    m: ( strings -- caddr u ) \ retrieve string from array at next index
	qty @ 0 >
	if
	    array @ index @ cell * + @ @$
	    index @ 1+ index !
	    index @ qty @ >=
	    if 0 index ! then 
	else 0 0 then ;m method @$x
    m: ( caddr u strings -- caddr1 u1 caddr2 u2 ) \ retrieve string from array at next index then
	\ split that next string at caddr u if possible
	\ caddr1 u1 is empty if caddr u string is not found
	\ caddr2 u2 contains the left over string if caddr u string is found
	qty @ 0 >
	if
	    array @ index @ cell * + @ split$ drop
	    index @ 1+ index !
	    index @ qty @ >=
	    if 0 index ! then 
	else 2drop 0 0 0 0 then ;m method split$s
    m: ( nstring-split$ nstring-source$ strings -- ) \ split up nstring-source$ with nstring-split$
	\ nstring-source$ is split each time nstring-split$ is found and place in this strings
	\ nstring-split$ is removed each time it is found and when no more are found process is done
	{ sp$ src$ }
	sp$ len$ 0 > src$ len$ 0 > and true =
	if
	    begin
		sp$ @$ src$ split$ true = if 2swap this !$x src$ !$ false else 2drop 2drop true then
	    until
	then ;m method split$to$s
    m: ( ncaddrfd u ncaddrsrc u1 -- ) \ split up ncaddrsrc u1 string with ncaddrfd u string
	\ same as split$to$s method but temporary strings are passed to this code
	\ ncaddrfd u is the string to find
	\ ncaddrsrc u1 is the string to find ncaddrfd u string in
	string heap-new string heap-new { fd$ src$ }
	src$ !$ fd$ !$
	fd$ src$ this split$to$s
	fd$ string-destruct src$ string-destruct ;m method split$>$s
    m: ( strings -- u ) \ report size of strings array
	strings-test @ strings-test =
	if qty @ else 0 then ;m method $qty
    m: ( strings -- ) \ reset index to start of strings list for output purposes
	0 index ! ;m method reset
    m: ( nstrings strings -- ) \ copy strings object to this strings object
	dup reset
	dup $qty 0 ?do dup @$x this !$x loop drop ;m method copy$s 
    m: ( string -- ) \ print object for debugging
	this [parent] print
	s" array:" type array @ .
	s" size:" type qty @ .
	s" iterate index:" type index @ . ;m overrides print
end-class strings

( \ some test words for memory leak testing 
0 value testing
0 value testb
: stringtest
    string heap-new to testing
    string heap-new to testb
    testing print cr
    testb print cr
    s" somestring !" testing !$
    testing @$ type cr
    s"  other string!" testing !+$
    testing @$ type cr
    s" just this string!" testing !$
    testing @$ type cr
    testing string-destruct testing free throw
    testb string-destruct testb free throw ;

: dotesting
    1000 0 ?do stringtest loop ;

0 value testc
: stringstest
    strings heap-new to testc
    s" hello world" testc !$x
    s" next string" testc !$x
    s" this is 2 or third item" testc !$x
    testc print cr
    testc $qty . cr
    testc @$x type cr
    testc @$x type cr
    testc @$x type cr
    testc strings-destruct
    testc free throw
;

: testall
    1000 0 ?do ." stringtest" cr stringtest ." stringstest" cr stringstest loop ;

)



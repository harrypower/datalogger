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
    cell% inst-var valid
    m: ( string -- ) \ initalize the string
	valid @ valid =
	if string-addr @ free throw then
	0 valid !
	0 string-addr !
	0 string-size ! ;m overrides construct
    m: ( string -- )     \ free allocated memory for this instance of object
	this construct   \ note this will only work when object made with heap-new
        this free throw ;m method destruct
    m: ( caddr u string -- ) \ store string
	valid @ valid =
	if
	    string-addr @ free throw
	    0 valid ! 
	then
	dup 0 >
	if
	    dup allocate throw
	    dup string-addr !
	    swap dup string-size ! move
	    valid valid !
	else 2drop
	then ;m method !$
    m: ( string -- caddr u ) \ retrieve string
	valid @ valid =
	if
	    string-addr @ string-size @
	else 0 0
	then ;m method @$
    m: ( caddr u string -- ) \ add a string to this string
	valid @ valid =
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
		valid valid !
	    else 2drop
	    then
	then ;m method !+$
    m: ( string -- u ) \ report string size
	valid @ valid =
	if string-size @ else 0 then ;m method len$ 
    m: ( string -- ) \ retrieve string object info
	this [parent] print
	s"  valid:" type valid @ valid = .
	s"  addr:" type string-addr @ .
	s"  size:" type string-size @ .
	s"  string:" type string-addr @ string-size @ type ;m overrides print
end-class string

object class
    cell% inst-var array \ contains first address of allocated string object
    cell% inst-var qty
    cell% inst-var index
    cell% inst-var valid
    m: ( strings -- ) \ initalize strings object
	valid @ valid =
	if
	    array @ 0 >
	    if \ deallocate memory allocated for the array
		qty @ 0 ?do array @ i cell * + @ destruct loop
		array @ free throw
	    then
	    0 qty !
	    0 index !
	    0 array !
	else
	    0 qty !
	    0 index !
	    0 array !
	    valid valid !
	then ;m overrides construct
    m: ( strings -- ) \ deallocate this object and other allocated memory in this object
	this construct
	this free throw ;m method destruct
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
    m: ( strings -- u ) \ report size of strings array
	valid @ valid =
	if qty @ else 0 then ;m method len$
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
    testing destruct
    testb destruct ;

: dotesting
    1000 0 ?do stringtest loop ;

0 value testc
: stringstest
    strings heap-new to testc
    s" hello world" testc !$x
    s" next string" testc !$x
    s" this is 2 or third item" testc !$x
    testc print cr
    testc len$ . cr
    testc @$x type cr
    testc @$x type cr
    testc @$x type cr
    testc destruct ;

: testall
    1000 0 ?do ." stringtest" cr stringtest ." stringstest" cr stringstest loop ;
)




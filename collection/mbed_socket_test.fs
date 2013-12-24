#! /usr/local/bin/gforth

include ../string.fs
include ../socket.fs
include ../Gforth-Tools/sqlite3_gforth_lib.fs
include errorlogging.fs

decimal 
4446 value mbed-port#
500000 value mbed-timeout#

0 value buffer
here to buffer 500 allot
variable buffer2$ buffer2$ $init
variable junk$ junk$ $init
variable temp$ temp$ $init

\ these are the errors that this code can produce!
struct
    cell% field socketfail-err
    cell% field mbedfail-err
    cell% field mbedpackage2termfail-err
    cell% field mbedpackagetermfail-err
    cell% field mbedhttpfail-err
    cell% field mbedmessagefail-err
    cell% field sockettime-err
end-struct errors%
create myerrors% errors% %allot drop
s" Data from mbed incomplete!" exception myerrors% mbedfail-err !             \ -2051
s" Socket failure in get-thp$ function" exception myerrors% socketfail-err !  \ -2052 
s" Mbed tcp socket package second terminator not present" exception myerrors% mbedpackage2termfail-err ! \ -2053
s" Mbed tcp socket terminator not present" exception myerrors% mbedpackagetermfail-err ! \ -2054
s" Mbed tcp HTTP header missing" exception myerrors% mbedhttpfail-err !       \ -2055
s" Mbed data message incomplete" exception myerrors% mbedmessagefail-err !    \ -2056
s" Socket timeout failure in mbedread-client" exception myerrors% sockettime-err ! \ -2057

struct
    cell% field http$
    cell% field socketterm$
    cell% field mbed-ip$ 
    cell% field mbed-dbname$
end-struct strings%

create mystrings% strings% %allot drop

s" HTTP/1.0 200 OK" mystrings% http$ $!
s\" \r\n\r\n" mystrings% socketterm$ $!
s" 192.168.0.116" mystrings% mbed-ip$ $!
s" sensordb.data" mystrings% mbed-dbname$ $!

: findsockterm ( caddr u -- caddr1 u1 nflag ) \ nflag is true if socket terminator is found and caddr1 u1 is a
    \ new string past the terminator.  String is the same as caddr u but the first part is removed as well as
    \ terminator
    \ nflag is false if there was no terminator found and caddr1 and u1 will contain original string 
    mystrings% socketterm$ $@ search
    if
	mystrings% socketterm$ $@ swap drop dup rot swap - rot rot + swap true
    else
	false
    then ;

: find2sockterm ( caddr u -- nflag ) \ looks for both socket terminators and returns true if it finds them
    findsockterm
    if
	findsockterm
	if
	    2drop true
	else
	    2drop false
	then
    else
	2drop false
    then ;

: mbedread-client ( caddr u nport# -- caddr1 u1 )
    open-socket { socketid }
    s\" GET /val \r\n\r\n" socketid write-socket
    buffer 499 erase
    utime
    begin
	2dup
	socketid buffer 499 read-socket  \ note read-socket is for TCP read-socket-from is for UDP
	2dup find2sockterm rot rot buffer2$ $!
	rot rot utime 2swap d- d>s mbed-timeout# >
	if
	    myerrors% sockettime-err @ throw
	then
    until
    2drop
    socketid close-socket
    buffer2$ $@
;

: get-thp$ ( -- caddr u nflag )  \ reads the mbed server and returns the data to be inserted into db
    try   \ flag is false for reading of mbed was ok any other value is some error
	mystrings% mbed-ip$ $@ mbed-port# mbedread-client
	mystrings% http$ $@ search
	if
	    mystrings% socketterm$ $@ search
	    if
		2dup 4 + mystrings% socketterm$ $@ search
		if
		    2drop 8 - swap 4 + swap false
		else 2drop 2drop myerrors% mbedpackage2termfail-err @ 
		then
	    else
		2drop myerrors% mbedpackagetermfail-err @ 
	    then
	else
	    2drop myerrors% mbedhttpfail-err @
	then
    restore dup if 0 swap 0 swap then
    endtry ;

: ck-thp$ ( caddr u -- nflag )  \ looks at string to see if all data is present by finding 4 comma's 
    0 { count }  \ if 4 comma's are found then false is returned true is returned if any other value is found
    0 ?do
	dup c@ ',' = if count 1+ to count then 1+
    loop drop
    count 4 = if false else myerrors% mbedmessagefail-err @ then ;


: !data ( caddr u -- )
    mystrings% mbed-dbname$ $@ dbname
    s" insert into thpdata values(NULL," temp$ $!
    datetime datetime$ $@ temp$ $+!
    temp$ $+!
    s" );" temp$ $+!
    temp$ $@ dbcmds
    sendsqlite3cmd throw
;

: gcs-thp$ ( -- )  \ get check store temperature humidity pressure string into database or throw errors
    get-thp$ dup 0 =
    if
	drop 2dup 
	ck-thp$ 0 =
	if  \ data from mbed checks out now put in database
	    !data
	else \ data is missing throw error
	    2drop mystrings% mbedfail-err @ throw
	then
    else
	2drop myerrors% socketfail-err @ throw
    then ;

: createdb ( -- )
    mystrings% mbed-dbname$ $@ dbname
    s" CREATE TABLE IF NOT EXISTS thpdata(row INTEGER PRIMARY KEY AUTOINCREMENT, year int,month int,day int, hour int, min int, sec int, age int,DTHtemperature int,DTHhumd int,BMPtemperature int, BMPpressure int);" dbcmds
    sendsqlite3cmd throw
    s" CREATE TABLE IF NOT EXISTS errors(row INTEGER PRIMARY KEY AUTOINCREMENT, year int,month int,day int,hour int,min int,sec int,error int);" dbcmds
    sendsqlite3cmd throw ;

: !error ( n -- nerror ) \ nerror is the returned error from sendsqlite3cmd false for ok anything else is an error
    mystrings% mbed-dbname$ $@ dbname
    s" insert into errors values(NULL," temp$ $!
    datetime datetime$ $@ temp$ $+!
    #to$ 1- temp$ $+!
    s" );" temp$ $+!
    temp$ $@ dbcmds
    sendsqlite3cmd  \ if sendsqlite3cmd produces and error here figure out how to handle it
;

: main_process ( -- nerror )
    TRY
	begin
	    gcs-thp$
	    30000 ms \ get the data every 30 seconds 
	again
    RESTORE dup if dup !error dup 0<> if !error drop else drop then then 
    ENDTRY ;

: main_loop ( -- )
    createdb
    begin
	main_process -28 = if true else false then \ bail only if user canceled program
    until ;

: see-db ( -- caddr u )
    mystrings% mbed-dbname$ $@ dbname
    s" select * from thpdata;" dbcmds
    sendsqlite3cmd throw dbret$ ;

: see-lastdb ( -- caddr u )
    mystrings% mbed-dbname$ $@ dbname
    s" SELECT * FROM thpdata LIMIT 2 OFFSET ((SELECT max(row) FROM thpdata) - 2);" dbcmds
    sendsqlite3cmd throw dbret$ ;

: see-dberrors ( -- caddr u )
    mystrings% mbed-dbname$ $@ dbname
    s" select * from errors;" dbcmds
    sendsqlite3cmd throw dbret$ ;

: see-lastdberrors ( -- caddr u )
    mystrings% mbed-dbname$ $@ dbname
    s" SELECT * FROM errors LIMIT 2 OFFSET ((SELECT max(row) FROM thpdata) - 2);" dbcmds
    sendsqlite3cmd throw dbret$ ;

: dto$ ( d -- caddr u )
    <<# #s #> #>> ;

: testsocketerror! ( nerror -- )
    s" testsocket.data" dbname
    s" insert into errors values(NULL," temp$ $!
    utime dto$ temp$ $+! s" ," temp$ $+!
    s>d dto$ temp$ $+!
    s" );" temp$ $+!
    temp$ $@ dbcmds
    sendsqlite3cmd throw ;

: testsocket ( -- dsockettime dtime )
    try
	utime mystrings% mbed-ip$ $@ mbed-port# mbedread-client type cr utime 2swap d- utime
	false
    restore dup
	if
	    testsocketerror! 0 s>d 0 s>d 
	else drop then
    endtry ;

: createtestdb ( -- )
    s" testsocket.data" dbname
    s" CREATE TABLE IF NOT EXISTS socketdata(row INTEGER PRIMARY KEY AUTOINCREMENT, time int,sockettime int);" dbcmds
    sendsqlite3cmd throw
    s" CREATE TABLE IF NOT EXISTS errors(row INTEGER PRIMARY KEY AUTOINCREMENT,time int, error int);" dbcmds
    sendsqlite3cmd throw ;

: testsocket! ( dsockettime dtime -- )
    s" testsocket.data" dbname
    s" insert into socketdata values(NULL," temp$ $!
    dto$ temp$ $+! s" ," temp$ $+!
    dto$ temp$ $+!
    s" );" temp$ $+!
    temp$ $@ dbcmds
    sendsqlite3cmd throw
;

: dosockettest ( -- )
    createtestdb
    begin
	testsocket testsocket! 5000 ms
    again ;

: seetestsocketdb ( -- caddr u )
    s" testsocket.data" dbname
    s" select * from socketdata limit 2 offset (( select max(row) from socketdata) -2);" dbcmds
    sendsqlite3cmd throw dbret$ ;

: seetestdberrors ( -- caddr u )
    s" testsocket.data" dbname
    s" select * from errors limit 2 offset (( select max(row) from errors) -2);" dbcmds
    sendsqlite3cmd throw dbret$ ;


: listtestdata ( -- )
    s" testsocket.data" dbname
    s" select max(row) from socketdata;" dbcmds
    sendsqlite3cmd throw dbret$
    s>number? 0 =
    if
	d>s 1 ?do
	    s" select * from socketdata limit 1 offset " junk$ $! i s>d dto$ junk$ $+! s" ;" junk$ $+!
	    junk$ $@ dbcmds
	    sendsqlite3cmd throw dbret$ type cr
	loop
    then
    ;
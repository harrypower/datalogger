#! /usr/local/bin/gforth

include ../string.fs
include ../socket.fs
include ../Gforth-Tools/sqlite3_gforth_lib.fs
include errorlogging.fs

decimal 
4446 value mbed-port#
2000000 value mbed-timeout#

0 value buffer
here to buffer 500 allot
variable buffer2$ buffer2$ $init
1000 value buff2max 
variable junk$ junk$ $init
variable temp$ temp$ $init

\ these are the errors that this code can produce!
s" Data from mbed incomplete!" exception constant mbedfail-err              \ -2052
s" Socket failure in get-thp$ function" exception constant socketfail-err   \ -2053 
s" Mbed tcp socket package second terminator not present" exception constant mbedpackage2termfail-err \ -2054
s" Mbed tcp socket terminator not present" exception constant mbedpackagetermfail-err \ -2055
s" Mbed tcp HTTP header missing" exception constant mbedhttpfail-err       \ -2056
s" Mbed data message incomplete" exception constant mbedmessagefail-err     \ -2057
s" Socket timeout failure in mbedread-client" exception constant sockettime-err \ -2058
s" Socket message recieved to large" exception constant tcpoverflow-err  \ -2059

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

: dto$ ( d -- caddr u )
    swap over dabs <<# #s rot sign #> #>> ;

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

: mbedread-client ( caddr u nport# -- caddr1 u1 nflag )
    try
	open-socket { socketid }
	s\" GET /val \r\n\r\n" socketid write-socket
	buffer 500 erase
	buffer2$ $init
	utime
	begin 
	    2dup
	    socketid buffer 499 read-socket  \ note read-socket is for TCP read-socket-from is for UDP
	    buffer2$ $+! 
	    utime 2swap d- d>s mbed-timeout# >
	    if
		socketid close-socket
		sockettime-err throw
	    then
	    buffer2$ $@ swap drop buff2max >
	    if
		socketid close-socket
		tcpoverflow-err throw
	    then
	    buffer2$ $@ find2sockterm
	until  
	2drop
	socketid close-socket
	buffer2$ $@ false
    restore  
	dup if swap drop then  
    endtry ;

: get-thp$ ( -- caddr u nflag )  \ reads the mbed server and returns the data to be inserted into db
    try   \ flag is false for reading of mbed was ok any other value is some error
	mystrings% mbed-ip$ $@ mbed-port# mbedread-client throw
	mystrings% http$ $@ search
	if
	    2dup find2sockterm
	    if
		findsockterm drop
		4 - false
	    else
		2drop mbedpackage2termfail-err 
	    then
	else
	    2drop mbedhttpfail-err  
	then
    restore dup if 0 swap 0 swap then 
    endtry ;

: ck-thp$ ( caddr u -- nflag )  \ looks at string to see if all data is present by finding 4 comma's 
    0 { count }  \ if 4 comma's are found then false is returned true is returned if any other value is found
    0 ?do
	dup c@ ',' = if count 1+ to count then 1+
    loop drop
    count 4 = if false else mbedmessagefail-err then ;

: ck-thp$zero ( caddr u -- nflag ) \ looks for a message from mbed of zeros
    s" 0, 0, 0," search swap drop swap drop ; \ returns true if zeros are found false otherwise

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
    get-thp$ dup false =
    if
	drop 2dup 2dup
	ck-thp$ false =
	if  \ data from mbed checks out now put in database
	    ck-thp$zero false =
	    if !data
	    else 2drop \ mbed resets and returns zero so do not store that
	    then	
	else \ data is missing throw error
	    2drop 2drop mbedfail-err throw
	then
    else
	throw
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
	    gcs-thp$ \ depth . cr
	    5000 ms  \ get the data every 30 seconds 
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

    
: listdberrors ( -- )
    mystrings% mbed-dbname$ $@ dbname
    s" select max(row) from errors;" dbcmds
    sendsqlite3cmd throw dberrmsg 2drop c@ 0<>
    if
	s" **sql msg**" type dberrmsg drop type
	begin
	    2 ms
	    sendsqlite3cmd throw dberrmsg 2drop c@ 0=
	until
    then
    dbret$  
    s>number? 0 =
    if
	d>s 0 { end now }  begin
	    s" select * from errors limit 1 offset " junk$ $! now s>d dto$ junk$ $+! s" ;" junk$ $+!
	    junk$ $@ dbcmds
	    sendsqlite3cmd throw dberrmsg 2drop c@ 0<>
	    if
		s" **sql msg**" type dberrmsg drop type
		begin
		    2 ms
		    sendsqlite3cmd throw dberrmsg 2drop c@ 0=
		until
		now 1- to now
	    else
		dbret$ type cr
	    then
	    now 1+ to now
	    now end >=
	until
    then
;

: listdbdata ( -- )
    mystrings% mbed-dbname$ $@ dbname
    s" select max(row) from thpdata;" dbcmds
    sendsqlite3cmd throw dberrmsg 2drop c@ 0<>
    if
	s" **sql msg**" type dberrmsg drop type
	begin
	    2 ms
	    sendsqlite3cmd throw dberrmsg 2drop c@ 0=
	until
    then
    dbret$
    s>number? 0 = 
    if
	d>s 1 { end now } begin
	    s" select * from thpdata limit 1 offset " junk$ $! now s>d dto$ junk$ $+! s" ;" junk$ $+!
	    junk$ $@ dbcmds
	    sendsqlite3cmd throw dberrmsg 2drop c@ 0<>
	    if
		s" **sql msg**" type dberrmsg drop type
		begin
		    2 ms
		    sendsqlite3cmd throw dberrmsg 2drop c@ 0=
		until
		now 1- to now
	    else
		dbret$ type cr
	    then
	    now 1+ to now
	    now end >=
	until
    then ;

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
    s" select max(row) from errors;" dbcmds
    sendsqlite3cmd throw dbret$
    s>number? 0 =
    if
	d>s 0 ?do
	    s" select * from errors limit 1 offset " junk$ $! i s>d dto$ junk$ $+! s" ;" junk$ $+!
	    junk$ $@ dbcmds
	    sendsqlite3cmd throw dbret$ type cr
	loop
    then
    ;
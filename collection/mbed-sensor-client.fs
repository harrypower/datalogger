#! /usr/bin/gforth

warnings off
next-exception @ value errorListStart
include ../string.fs
include ../socket.fs
include ../Gforth-Tools/sqlite3_gforth_lib.fs

decimal 
4446 value mbed-port#
2000000 value mbed-timeout#
60000 value mbed-readtime

0 value buffer
here to buffer 500 allot
variable buffer2$ buffer2$ $init
1000 value buff2max 
variable junk$ junk$ $init
variable temp$ temp$ $init

\ these are the errors that this code can produce!
\ Note these are enumerated starting at -2051 but -2051 is in another module so the enumeration is first one gets -2051
s" Data from mbed incomplete!"                            exception constant mbedfail-err              \ -2052
s" Socket failure in get-thp$ function"                   exception constant socketfail-err            \ -2053 
s" Mbed tcp socket package second terminator not present" exception constant mbedpackage2termfail-err  \ -2054
s" Mbed tcp socket terminator not present"                exception constant mbedpackagetermfail-err   \ -2055
s" Mbed tcp HTTP header missing"                          exception constant mbedhttpfail-err          \ -2056
s" Mbed data message incomplete"                          exception constant mbedmessagefail-err       \ -2057
s" Socket timeout failure in mbedread-client"             exception constant sockettime-err            \ -2058
s" Socket message recieved to large"                      exception constant tcpoverflow-err           \ -2059
next-exception @ value errorListEnd

struct
    cell% field http$
    cell% field socketterm$
    cell% field mbed-ip$ 
    cell% field mbed-dbname$
end-struct strings%

create mystrings% strings% %allot drop

s" HTTP/1.0 200 OK"  mystrings% http$ $!
s\" \r\n\r\n"        mystrings% socketterm$ $!
\ s" 192.168.0.116"    mystrings% mbed-ip$ $!
s" harrypi.dlinkddns.com" mystrings% mbed-ip$ $!
s" sensordb.data"    mystrings% mbed-dbname$ $!

: error#to$ ( nerror -- caddr u )  \ takes an nerror number and gives the string for that error
    >stderr Errlink   \ this may only work in gforth ver 0.7 and may only work with use exceptions made 
    begin             \ if the nerror does not exist then a null string is returned!
	@ dup
    while
	    2dup cell+ @ =
	    if
		2 cells + count rot drop exit
	    then
    repeat ;

: dto$ ( d -- caddr u )  \ convert double signed to a string
    swap over dabs <<# #s rot sign #> #>> ;

: #to$ ( n -- c-addr u1 ) \ convert n to string then add a "," at the end of the converted string
    s>d
    swap over dabs
    <<# #s rot sign #> #>>
    junk$ $! s" ," junk$ $+! junk$ $@ ;

: datetime$ ( -- caddr u ) \ create the current time value of unixepoch and make into a string with a "," at the end of string
    utime 1000000 fm/mod swap drop
    #to$ ;

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
    datetime$ temp$ $+!
    temp$ $+!
    s" );" temp$ $+!
    temp$ $@ dbcmds
    sendsqlite3cmd throw  \ note if this throws the data is lost for this reading ... maybe changed that behavior
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

: createdb ( -- )  \ creates the database if it is not present already 
    mystrings% mbed-dbname$ $@ dbname   \ **** note this word needs to be changed to deal with the errors it can produce ******
    s" CREATE TABLE IF NOT EXISTS thpdata(row INTEGER PRIMARY KEY AUTOINCREMENT,dtime INTEGER, age INT,DTHtemperature INT,DTHhumd INT,BMPtemperature INT, BMPpressure INT);" dbcmds
    sendsqlite3cmd throw
    s" CREATE TABLE IF NOT EXISTS errors(row INTEGER PRIMARY KEY AUTOINCREMENT,dtime INTEGER,error INT);" dbcmds
    sendsqlite3cmd throw
    s" CREATE TABLE IF NOT EXISTS errorList(error INT UNIQUE,errorText TEXT);" dbcmds
    sendsqlite3cmd throw
;

: !error ( n -- nerror ) \ nerror is the returned error from sendsqlite3cmd false for ok anything else is an error
    mystrings% mbed-dbname$ $@ dbname
    s" insert into errors values(NULL," temp$ $!
    datetime$ temp$ $+!
    #to$ 1- temp$ $+!
    s" );" temp$ $+!
    temp$ $@ dbcmds
    sendsqlite3cmd  \ this may return non zero and should be thrown but not sure how to deal with this yet!
;

: errorINlist? { nsyserror -- nflag ndberror }  \ see if nsyserror number is in database list of errors
    \ ndberror is false only if there was no dberror if ndberror is true then nflag is undefined
    \ nflag is true if nsyserror is in the dbase
    \ nflag is false if nsyserror is not in the dbase
    mystrings% mbed-dbname$ $@ dbname  
    s" select exists (select error from errorList where (error = " temp$ $!
    nsyserror #to$ 1 - temp$ $+!
    s" ));" temp$ $+! temp$ $@ dbcmds
    10 1 ?do
	sendsqlite3cmd 0= if leave else i 20 * ms then
    loop
    dbret$ s" 1" search swap drop swap drop dup 
    dbret$ s" 0" search swap drop swap drop xor false =
    if
	true
    else
	dberrmsg swap drop swap drop 
    then ;

: puterrorINlist ( nerror -- ) \ add nerror number and string to the error list
    mystrings% mbed-dbname$ $@ dbname
    dup -2 <>
    if
	s" insert into errorList values(" temp$ $!
	dup  #to$ temp$ $+! s\" \"" temp$ $+!
	error#to$ temp$ $+! s\" \");" temp$ $+! temp$ $@ dbcmds
    else
	drop 
	s\" insert into errorList values(-2, \'Abort\" has occured!\');" dbcmds
    then
    sendsqlite3cmd drop \ ****note if there is a sqlite3 error here this new error will not be stored in list*****
;

: !errorlist { nerror -- } \ will add the nerror associated string for that error to database
    10 1 ?do
	nerror errorINlist?
	false =
	if false =
	    if nerror puterrorINlist then
	    leave
	else drop i 20 * ms
	then
    loop ;

: main_process ( -- nerror )
    TRY
	begin
	    gcs-thp$ \ depth . cr
	    mbed-readtime  ms  \ get the data every 60 seconds 
	again
    RESTORE
	dup dup !errorlist
	if dup !error dup 0<> if 100 ms !error drop else drop then then 
    ENDTRY ;

: main_loop ( -- )
    createdb  \ *** currently this word throws if cant talk to sqlite3 and create db... trap these errors and do something better then that! ***
    begin
	main_process -28 = if true else false then \ bail only if user canceled program
	mbed-readtime ms \ wait for next read time 
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

: see-lastdberrorlist ( -- caddr u )
    mystrings% mbed-dbname$ $@ dbname
    s" select * from errorList limit 1 offset (select min(error) from errorList);" dbcmds
    sendsqlite3cmd throw dbret$ ;
    
: listdberrors ( -- )
    mystrings% mbed-dbname$ $@ dbname
    s" select max(row) from errors;" dbcmds
    sendsqlite3cmd 0<> 
    if
	s" **sql msg**" type dberrmsg drop type
	begin
	    2 ms
	    sendsqlite3cmd 0= 
	until
    then
    dbret$  
    s>number? 0 =
    if
	d>s 0 { end now }  begin
	    s" select row,datetime(dtime,'unixepoch'),error from errors limit 1 offset " junk$ $!
	    now s>d dto$ junk$ $+! s" ;" junk$ $+!
	    junk$ $@ dbcmds
	    sendsqlite3cmd 0<> 
	    if
		s" **sql msg**" type dberrmsg drop type
		begin
		    2 ms
		    sendsqlite3cmd 0=
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
    sendsqlite3cmd 0<> 
    if
	s" **sql msg**" type dberrmsg drop type
	begin
	    2 ms
	    sendsqlite3cmd 0= 
	until
    then
    dbret$
    s>number? 0 = 
    if
	d>s 0 { end now } begin
	    s" select row,datetime(dtime,'unixepoch'),age,DTHtemperature,DTHhumd,BMPtemperature,BMPpressure from thpdata limit 1 offset " junk$ $!
	    now s>d dto$ junk$ $+! s" ;" junk$ $+!
	    junk$ $@ dbcmds
	    sendsqlite3cmd 0<> 
	    if
		s" **sql msg**" type dberrmsg drop type
		begin
		    2 ms
		    sendsqlite3cmd 0= 
		until
		now 1- to now
	    else
		dbret$ type cr
	    then
	    now 1+ to now
	    now end >=
	until
    then ;

: config-mbed-client ( -- ) \ will run when this file is loaded and will look at arguments for operation
    next-arg dup 0=
    if
	." Argument needed!" cr 2drop s" -help"
    then

    s" -help" search
    if
	." -r use to start the datalogging process!" cr
	." -i use to enter the gforth command line to issue commands!" cr
	2drop bye
    then
    s" -r" search
    if
	2drop main_loop bye
    then
    s" -i" search
    if
	2drop \ now just enter the gforth cmd line
    else
	2drop
	." Switch not supported!" cr
	." -r use to start the datalogging process!" cr
	." -i use to enter the gforth command line to issue commands!" cr
	bye
    then
    ;

    config-mbed-client
    

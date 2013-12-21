#! /usr/local/bin/gforth

include ../string.fs
include ../socket.fs
include ../Gforth-Tools/sqlite3_gforth_lib.fs
include errorlogging.fs

decimal 
4446 value mbed-port#

0 value buffer
here to buffer 500 allot
variable junk$ junk$ $init
variable temp$ temp$ $init


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

: mbedread-client ( caddr u nport# -- caddr1 u1 )
    open-socket { socketid }
    s\" GET /val \r\n\r\n" socketid write-socket
    0 0 begin
	2drop
 	socketid buffer 499 read-socket  \ note read-socket is for TCP read-socket-from is for UDP
	2dup s\" \r\n\r\n" search swap drop swap drop
	\ this will bail if it finds end cr lf cr lf or it will generate error 
    until
    socketid close-socket ;

: get-thp$ ( -- caddr u nflag )  \ reads the mbed server and returns the data to be inserted into db
    try   \ flag is false for reading of mbed was ok true means reading failed for some reason
	mystrings% mbed-ip$ $@ mbed-port# mbedread-client
	mystrings% http$ $@ search
	if
	    mystrings% socketterm$ $@ search
	    if
		2dup 4 + mystrings% socketterm$ $@ search
		if 2drop 8 - swap 4 + swap false else 2drop 2drop true then
	    else
		2drop true
	    then
	else
	    2drop true
	then
    restore dup if 0 swap 0 swap then
    endtry ;

: ck-thp$ ( caddr u -- nflag )  \ looks at string to see if all data is present by finding 4 comma's 
    0 { count }  \ if 4 comma's are found then false is returned true is returned if any other value is found
    0 ?do
	dup c@ ',' = if count 1+ to count then 1+
    loop drop
    count 4 = if false else true then ;


: !data ( caddr u -- )
    mystrings% mbed-dbname$ $@ dbname
    s" insert into thpdata values(" temp$ $!
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
	    2drop s" Data from mbed incomplete!" exception throw
	then
    else
	2drop s" Socket failure in get-thp$ function" exception throw
    then ;

: createdb ( -- )
    mystrings% mbed-dbname$ $@ dbname
    s" CREATE TABLE IF NOT EXISTS thpdata(year int,month int,day int, hour int, min int, sec int, age int,temp int,humd int,btemp int, pressure int);" dbcmds
    sendsqlite3cmd throw
    s" CREATE TABLE IF NOT EXISTS errors(year int,month int,day int,hour int,min int,sec int,error int);" dbcmds
    sendsqlite3cmd throw ;

: see-db ( -- caddr u )
    mystrings% mbed-dbname$ $@ dbname
    s" select * from thpdata;" dbcmds
    sendsqlite3cmd throw
    dbret$ ;

: see-dberrors ( -- caddr u )
    mystrings% mbed-dbname$ $@ dbname
    s" select * from errors;" dbcmds
    sendsqlite3cmd throw
    dbret$ ;

: !error ( n -- nerror ) \ nerror is the returned error from sendsqlite3cmd false for ok anything else is an error
    mystrings% mbed-dbname$ $@ dbname
    s" insert into errors values(" temp$ $!
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

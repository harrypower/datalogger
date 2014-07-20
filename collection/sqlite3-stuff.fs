\ This Gforth code is a Raspberry Pi Data logging code
\    Copyright (C) 2014  Philip K. Smith

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

\ This code is the sqlite3 interface type words for this database project
\ What should be in here are words to put in and take out of database!


\ warnings off

require string.fs
require ../Gforth-Tools/sqlite3_gforth_lib.fs
require gforth-misc-tools.fs

decimal

variable db-path$ 
variable temp$

100 constant sqlite3-resend-time    \ this codes sqlite3 routines will wait for this time in ms if a locked database is found befor resending cmds

path$ $@ db-path$ $! s" /collection/datalogged.data" db-path$ $+!  \ this is the name of the database

\ These are the enumerated errors this code can produce
next-exception @ constant sqlite-errorListStart  \ this is start of enumeration of errors for this code

s" Registration string not present in create-datalogging-table!"                 exception constant dltable-name-er
s" Registration string empty or formed incorrectly in parse-new-device-json!"    exception constant parse-new-er
s" Table name already present in database file! (change name of table)"          exception constant table-present-er
s" No data node's present when making table registration!"                       exception constant no-data-node-er
s" Registry data not recieved from device!"                                      exception constant wg-registry-er
s" Registration parsing data node quantitys do not match!"                       exception constant parse-quantity-er
s" Data table name is not present in parse-data-table!"                          exception constant datatable-name-er
s" Ip address aready registered in database!"                                    exception constant ip-already-er
s" Sensor data parsing json error!"                                              exception constant data-parse-er
s" No data to parse from sensor error!"                                          exception constant no-data-er
s" Data quantity not present from sensor!"                                       exception constant data-quantity-er
s" Sensor data quantity does not match table quantity!"                          exception constant data-table-quantity-er
s" Data quantity not retreaved from device table!"                               exception constant quantity-retreave-er
s" Sensor json data quantity does not match registered device quantity!"         exception constant json-quantity-er
s" Table does not exist to get the fields of!"                                   exception constant field->table-err
s" ErrorList query for an error did not return expected number!"                 exception constant errorlist-number-err
next-exception @ constant sqlite-errorListEnd    \ this is end of enumeration of errors for this code

: setupsqlite3 ( -- ) \ sets default stuff up for sqlite3 work
    initsqlall
    db-path$ $@ dbname ;

: dberrorthrow ( nerror -- ) \ locked database type errors and buffer overflow error will cause a wait and a resend
    \ if a resend sqlite3 cmd fails then that error is thrown
    \ all other errors will throw that are behond the scope of recovery in this code!
    case
	5     of sqlite3-resend-time ms sendsqlite3cmd throw  endof \ database is locked now
	6     of sqlite3-resend-time ms sendsqlite3cmd throw  endof \ table is locked now
	sqlerrors retbuffover-err @
	      of sqlmessg retbuffmaxsize-cell @ 2 * mkretbuff sendsqlite3cmd throw  endof \ buffer overflow from sqlite3
	\ might want to limit this buffer resize to 10k bytes or something like that
	dup throw
    endcase ;

2157 constant db-ok
2156 constant db-errors
: sqlite-integrity-check ( -- nflag ) \ This will do a pragma integrity_check and test returned result
    \ nflag is db-ok if that is the returned result from sqlite3
    \ nflag is db-errors is returned if ok was not returned from sqlite3
    \ nflag can also return other system or sqlite3 messages
    try
	setupsqlite3
	s" pragma integrity_check;" dbcmds
	sendsqlite3cmd dberrorthrow
	dbret$ s" ok" search -rot 2drop true = if db-ok throw then
	dbret$ s" ok" search -rot 2drop false = if db-errors throw then 
    restore 
    endtry ;

2159 constant table-yes
2158 constant table-no
: sqlite-table? ( caddr u -- nflag ) \ will search db for the string as a table name.  
    \ nflag is table-yes (2159) meaning the table is in database
    \ nflag is table-no  (2158) meaning the table is not in database
    \ nflag is some error either  +1 to +110 for some sqlite3 error
    \ nflag is some error either -1 to -x for some system error or other returned error defined with exception
    try
	{ caddr u }
	setupsqlite3
	s" " dbfieldseparator
	s" " dbrecordseparator
	s" select name from sqlite_master where name = '" temp$ $! caddr u temp$ $+! s" ';" temp$ $+! temp$ $@ dbcmds
	sendsqlite3cmd dberrorthrow 
	dbret$ caddr u search -rot 2drop true = if table-yes throw else table-no throw then
    restore swap drop swap drop   
    endtry ;

2160 constant ip-yes
2161 constant ip-no
: sqlite-ip? ( caddrip u -- nflag ) \ search device table for the ip addr in the string.
    \ nflag is ip-no (2161) if no ip address registered yet
    \ nflag is ip-yes (2160) if ip address is registered now
    \ nflag can return other numbers indicating sqlite3 errors or system errors
    try
	{ caddr u }
	setupsqlite3
	s" " dbfieldseparator
	s" " dbrecordseparator
	s" select ip from devices where ip = '" temp$ $! caddr u temp$ $+! s" ';" temp$ $+! temp$ $@ dbcmds
	sendsqlite3cmd dberrorthrow
	dbret$ caddr u search -rot 2drop true = if ip-yes throw else ip-no throw then
    restore swap drop swap drop  
    endtry ;

2162 constant dname-yes
2163 constant dname-no
: sqlite-devicename? ( caddrname u -- nflag ) \ search device table and database for device name or table name
    \ nflag is dname-yes (2162) if a named device is registered
    \ nflag is dname-no  (2163) if there is no named device registered
    \ nflag can return other numbers indicating sqlite3 errors or system errors
    try
	{ caddr u }
	setupsqlite3
	s" " dbfieldseparator
	s" " dbrecordseparator
	s" select data_table from devices where data_table = '" temp$ $! caddr u temp$ $+! s" ';" temp$ $+! temp$ $@ dbcmds
	sendsqlite3cmd dberrorthrow
	dbret$ caddr u search -rot 2drop true = if dname-yes else dname-no then
	dup dname-no =
	if
	    drop
	    caddr u sqlite-table? dup table-yes =
	    if drop dname-yes throw else dup table-no = if drop dname-no throw else throw  then then 
	then
	false
    restore dup if swap drop swap drop else drop then 
    endtry ;

: create-device-table ( -- ) \ This creates the main table used in the database. This table is used to reference all other tables and then data in database.
    \ table is called devices
    \ row is primary key and is autoincrmented
    \ dt_added is the date time stamp device was added
    \ ip address of the device
    \ port number of the device
    \ method would contain the string to send socket device to get data from device eg "val"
    \ data_table is the name of the table that contains the logged data for this registered device
    \ read_device is yes or no.  no means device is not read anymore. yes means device is read from still.
    \ store_data is yes or no. No means data_list_id table is not to be writen to anymore.
    \      Yes means data_list_id table is to be writen to still when device is read from.
    \ quantity is the size of the fields in the data_table that will contain the data from the logged device
    setupsqlite3
    s" CREATE TABLE IF NOT EXISTS devices(row INTEGER PRIMARY KEY AUTOINCREMENT,dt_added INTEGER," temp$ $!
    s" ip TEXT,port TEXT,method TEXT,data_table TEXT," temp$ $+!
    s" read_device TEXT,store_data TEXT,quantity INTEGER );" temp$ $+! temp$ $@
    dbcmds
    sendsqlite3cmd dberrorthrow ;

: create-error-tables ( -- ) \ used to create the error logging tables 
    setupsqlite3
    s" CREATE TABLE IF NOT EXISTS errors(row INTEGER PRIMARY KEY AUTOINCREMENT,dtime INTEGER,error INT);" dbcmds
    sendsqlite3cmd dberrorthrow
    setupsqlite3
    s" CREATE TABLE IF NOT EXISTS errorList(error INT UNIQUE,errorText TEXT);" dbcmds
    sendsqlite3cmd dberrorthrow ;

: error-sqlite3! ( nerror -- ) \ used to store error values into errors table
    try
	setupsqlite3
	s" insert into errors values(NULL," temp$ $!
	datetime$ temp$ $+!
	#to$ temp$ $+!
	s" );" temp$ $+!
	temp$ $@ dbcmds
	sendsqlite3cmd  dberrorthrow
	false
    restore if drop then \ note this word will not pass any errors upwards so nothing is returned !
    endtry ;

2164 constant error-yes
2165 constant error-no
: (errorINlist?) { nsyserror -- nflag }  \ see if nsyserror number is in database list of errors
    \ nflag is error-yes if nsyserror is in the dbase
    \ nflag is error-no if nsyserror is not in the dbase
    \ nflag can be other error numbers
    try
	setupsqlite3
	s" " dbfieldseparator
	s" " dbrecordseparator
	s" select error from errorList where (error = " temp$ $!
	nsyserror #to$ temp$ $+!
	s" );" temp$ $+! temp$ $@ dbcmds
	sendsqlite3cmd dberrorthrow 
	dbret$ dup 0 =
	if
	    \ the error is not in the list at this time
	    2drop error-no throw
	else
	     s>number? false =
	     if
		\ number not recieved from errorList query
	 	2drop errorlist-number-err throw
	     else 
	 	 d>s nsyserror <>
		 if
		     errorlist-number-err throw
		 else
		     error-yes throw
		 then
	    then
	then
    restore 
    endtry ;

: (puterrorINlist) ( nerror -- ) \ add nerror number and string to the error list
    setupsqlite3
    dup -2 <>  \ this is needed because -2 error does not report a message even though it is Abort !
    if
	s" insert into errorList values(" temp$ $!
	dup  #to$, temp$ $+! s\" \"" temp$ $+!
	error#to$ temp$ $+! s\" \");" temp$ $+! temp$ $@ dbcmds
    else
	drop
	s\" insert into errorList values(-2, \'Abort\" has occured!\');" dbcmds
    then
    sendsqlite3cmd drop \ ****note if there is a sqlite3 error here this new error will not be stored in list*****
    \ **** positive error numbers can come from sqlite3 but the proper string may not get placed in db for the number
;

: errorlist-sqlite3! ( nerror -- ) \ will add the nerror associated string for that error to database 
    0 { nerror ntest -- } 
    try
	5 0 ?do \ try to test and store 5 times after that just bail
	    nerror (errorINlist?) to ntest ntest error-no =
	    if
		nerror (puterrorINlist) leave
	    then
	    ntest error-yes <>
	    if
		i 20 * ms
	    else
		leave
	    then
	loop
	false 
    restore drop \ nothing returned by this word even if an error happens in this word!	
    endtry ;

struct
    cell% field next-node \ 0 indicates no more nodes
    cell% field data-id$
    cell% field data-type$
end-struct data-node%

struct
    cell% field dt_added$
    cell% field ip$
    cell% field port$
    cell% field method$
    cell% field data_table$
    cell% field read_device$
    cell% field store_data$
    cell% field data-node
    cell% field data-quantity$
end-struct device%

create new-device
device% %size allot new-device device% %size erase
variable parse-junk$

false value name-type?  \ this is false for name is next and true for type is next 
0 value node-addr  \ this is set when a name is placed because the name makes the node
0 value node-count \ this is used to confirm data node count created matches info of sent quantity
: cleanup$ ( caddr u addr -- caddr1 u1 addr ) -rot '"' skip -trailing 1 - rot ;
: pjson-"sensor_name" ( caddr u addr -- ) cleanup$ data_table$ $! ;
: pjson-"ip"          ( caddr u addr -- ) cleanup$ ip$ $! ;
: pjson-"port"        ( caddr u addr -- ) cleanup$ port$ $! ;
: pjson-"method"      ( caddr u addr -- ) cleanup$ method$ $! ;
: pjson-"quantity"    ( caddr u addr -- ) data-quantity$ $! ; \ note this is stored here as a string but is a int in database
: pjson-              ( caddr u addr -- ) parse-new-er throw ;  \ incorrect formed json found
: (make-data-node)    ( caddr -- )  \ allots data node space and saves addres in the caddr provide
    data-node% %allot dup data-node% %size erase swap ! ;
: (create-data-node)  ( caddr -- caddr1 ) \ returns address of the current working node 
    data-node @ 0 =
    if   \ makes first node and returns address for location of that node
	new-device data-node (make-data-node)
	new-device data-node @
    else \ iterate to end of nodes and make another one and return its address
	new-device data-node @ \ start at first location
	begin
	    dup next-node @ dup if swap drop false else drop true then
	until
	dup next-node (make-data-node)
	next-node @
    then ;

: pjson-"name"        ( caddr u addr -- )
    cleanup$ 
    name-type? false <> if parse-new-er throw then
    swap dup 0 = if parse-new-er throw then
    swap (create-data-node) dup to node-addr data-id$ $!
    true to name-type?
    node-count 1 + to node-count ;

: pjson-"type"        ( caddr u addr -- )
    cleanup$
    drop \ this addres is not used must calculate real addr to store this type
    name-type? true <> if parse-new-er throw then
    dup 0 = if parse-new-er throw then
    node-addr 0 = if parse-new-er throw then
    node-addr data-type$ $!
    false to name-type?
    0 to node-addr ;

: (parse-json) ( caddr u -- )
    ':' $split 
    2swap s" pjson-" temp$ $! temp$ $+! temp$ $@ find-name name>int new-device swap execute ;

variable register_device$
variable register_data$
: parse-new-device-json ( caddr u - nflag ) \ string is the json to register this sensor. nflag is false when json data parsed correctly and in structure now
    try
	dup 0 =
	if
	    parse-new-er throw
	else
	    false to name-type?  \ start with a name
	    0 to node-addr  \ start with no addr
	    0 to node-count \ start with 0 nodes
	    temp$ $!
	    temp$ $@ s\" {\"register device\":" search false = if parse-new-er throw then
	    '{' skip '{' scan '{' skip register_device$ $!
	    temp$ $@ s\" \"register data\":" search false = if parse-new-er throw then
	    '{' scan '{' skip register_data$ $!
	    register_device$ $@ dup -rot '}' scan swap drop - register_device$ $!len
	    register_data$ $@ '}' scan drop register_data$ $@ drop - register_data$ $!len
	    register_device$ ',' ['] (parse-json) $iter
	    register_data$ ',' ['] (parse-json) $iter
	    datetime$ 1 - new-device dt_added$ $!
	    s" yes" new-device store_data$ $!
	    s" yes" new-device read_device$ $!
	    new-device data-quantity$ $@ s>unumber? true =
	    if
		d>s node-count <> if parse-quantity-er throw then
	    else
		parse-quantity-er throw
	    then
	    false
	then 
    restore dup if swap drop swap drop then
    endtry ;

: view-new-device-data ( -- )
    cr
    new-device dt_added$ $@ type cr
    new-device ip$ $@ type cr
    new-device port$ $@ type cr
    new-device method$ $@ type cr
    new-device data_table$ $@ type cr
    new-device read_device$ $@ type cr
    new-device store_data$ $@ type cr
    new-device data-node @ . cr 
    new-device data-quantity$ $@ type cr ;
: view-new-data-node ( addr -- )
    dup data-id$ $@ type dup s"  " type data-type$ $@ type next-node @ .s ;

variable makedn$
: make-data-node-string ( -- cadrr u )
    new-device data-node @ dup 0 <>
    if
	makedn$ $off makedn$ init$ 
	begin
	    dup data-id$ $@ makedn$ $+! s"  " makedn$ $+! dup data-type$ $@ makedn$ $+! s" ," makedn$ $+!
	    next-node @ dup 0 =
	until
	drop makedn$ $@
    else
	drop no-data-node-er throw
    then ;

: create-datalogging-table ( -- nflag ) \ nflag will be false if no errors
    try
	setupsqlite3
	new-device data_table$ $@len 0 = if dltable-name-er throw then
	new-device data-node @ 0 = if dltable-name-er throw then
	s" CREATE TABLE " temp$ $!
	new-device data_table$ $@ temp$ $+!
	s" (row INTEGER PRIMARY KEY AUTOINCREMENT,dtime INTEGER," temp$ $+!
	make-data-node-string 1 - temp$ $+! \ remove the , from the string
	s" );" temp$ $+! temp$ $@ dbcmds
	sendsqlite3cmd dberrorthrow
	false
    restore 
    endtry ;

: create-device-entry ( -- )
    setupsqlite3
    s" insert into devices values(NULL," temp$ $! datetime$ temp$ $+!
    s" '" temp$ $+! new-device ip$ $@ temp$ $+! s" ','" temp$ $+!
    new-device port$ $@ temp$ $+! s" ','" temp$ $+!
    new-device method$ $@ temp$ $+! s" ','" temp$ $+!
    new-device data_table$ $@ temp$ $+! s" ','" temp$ $+!
    new-device read_device$ $@ temp$ $+! s" ','" temp$ $+!
    new-device store_data$ $@ temp$ $+! s" '," temp$ $+!
    new-device data-quantity$ $@ temp$ $+! s" );" temp$ $+! \ data_quantity$ is interger in the database so no ' are needed!
    temp$ $@ dbcmds sendsqlite3cmd dberrorthrow ;

: register-device-$ ( caddr u -- nflag ) \ will register a new device into database device table if there are no conflics
    try  \ nflag will be false if new device registered and is now in database to be used
	new-device data-node off   \ note every time this code runs to parse a new device there will be small memory leak
	new-device dt_added$ dup $off init$
	new-device ip$ dup $off init$
	new-device port$ dup $off init$
	new-device method$ dup $off init$
	new-device data_table$ dup $off init$
	new-device read_device$ dup $off init$
	new-device store_data$ dup $off init$
	new-device data-quantity$ dup $off init$
	create-device-table
	create-error-tables
	parse-new-device-json throw
	new-device ip$ $@ sqlite-ip? dup ip-yes =
	if ip-already-er throw else dup ip-no <> if throw else drop then then
	new-device data_table$ $@ sqlite-devicename? dup dname-yes =
	if table-present-er throw else dup dname-no <> if throw else drop then then 
	create-datalogging-table throw
	create-device-entry
	false
    restore dup 
	if
	    swap drop swap drop \ clean up after error
	then
    endtry ;

: get-register-$ ( caddr-ip u -- nflag ) \ takes a string that has ip and port numbers to get registration data from
    \ eg s" 192.168.0.126:4445" could be used to talk to a sensor at that ip address and that port number
    s" sudo wget --output-document=" temp$ $! path$ $@ temp$ $+! s" /collection/wg-reg-device.data " temp$ $+! 
    temp$ $+! s" /regdev" temp$ $+! temp$ $@ system
    path$ $@ temp$ $! s" /collection/wg-reg-device.data" temp$ $+! temp$ $@ slurp-file dup 0 >
    if
	register-device-$  
    else
	wg-registry-er 
    then ;

struct
    cell% field next-data-node
    cell% field name$
    cell% field value$
end-struct parse-data%

variable parsed-data 
parse-data% %allocate throw parsed-data ! \ the last node is always empty this starts that empty node 
parsed-data @ parse-data% %size 0 fill \ erase  \ this ensures the test for last node will show it as last node

0 value current-data-quantity
: free-parsed-data ( -- nflag ) \ remove nodes from memory and free strings also
    try  \ nflag will be false indicating all is ok
	parsed-data @ next-data-node @ 0 <> \ detect if there are any nodes to free!  The last node is never freed!
	if
	    parsed-data @ { cpd } \ this is the first node location
	    begin
		cpd name$ $off 
		cpd value$ $off 
		cpd next-data-node @ dup 0 =  \ get next node address and loop exit state calculate
		cpd free throw  \ free this node
		swap to cpd
	    until
	    parse-data% %allocate throw parsed-data ! \ create the empty node for next use
	    parsed-data @ parse-data% %size 0 fill \ erase \ clear it to ensure last node test will work
	then
	false
    restore 
    endtry ;

: make-data-node     ( -- caddr ) \ make a new node for data and return its address
    parse-data% %allocate throw
    dup parse-data% %size erase
    parsed-data @ { caddr working-addr }
    begin
	working-addr next-data-node @ 0 =
	if
	    caddr working-addr next-data-node ! true
	else
	    working-addr next-data-node @ to working-addr false
	then
    until
    working-addr ;

: find-last-data-node ( -- caddr ) \ return the address of the current node that is to be added to
    parsed-data @ 0 { working-addr last-addr }
    begin
	working-addr next-data-node @ 0 =
	if
	    true
	else
	    working-addr to last-addr 
	    working-addr next-data-node @ to working-addr false
	then
    until last-addr ;

: view-parsed-data ( -- ) \ simply view the data in the current data node if there are any
    cr parsed-data @ { working-addr } \ this helper word will only show if the data was not freed already
    begin
	working-addr next-data-node @ 0 =
	if
	    true
	else
	    working-addr next-data-node @ . cr
	    working-addr name$ $@ type cr
	    working-addr value$ $@ type cr
	    working-addr next-data-node @ to working-addr
	    false
	then
    until ;

: pjsdata-"name"     ( caddr u -- ) '"' scan '"' skip '"' $split 2drop make-data-node name$ $! ;
: pjsdata-"value"    ( caddr u -- )
    find-last-data-node dup 0 <>
    if
	value$ $!
	current-data-quantity 1 + to current-data-quantity
    else
	data-quantity-er throw
    then ;
: pjsdata-           ( caddr u -- ) data-parse-er throw ;

: (parse-json-data) ( caddr u -- )
    ':' $split
    2swap s" pjsdata-" temp$ $! temp$ $+! temp$ $@ find-name name>int execute ;

0 value data-quantity
variable data-parse$
: (parse-data-table) ( caddr u -- nflag )  \ nflag is false if the data was parsed and in data nodes now
    \ note parsing will work as long as quantity reported in json is same as quantity sent in json.
    \ no checking is done to see if data matches what should be stored
    try
	dup 0 =
	if
	    no-data-er throw
	else
	    s\" {\"quantity\":" search false = if data-quantity-er throw then
	    ':' scan ':' skip temp$ $!
	    temp$ $@ ',' scan swap drop temp$ $@len swap -
	    temp$ $@ rot swap drop s>unumber?
	    true <> if data-quantity-er throw then
	    d>s to data-quantity
	    0 to current-data-quantity
	    temp$ $@ '{' scan  '{' skip '}' $split 2drop data-parse$ $! \ this removes the { at front of string and }} at end of string
	    data-parse$ ',' ['] (parse-json-data) $iter
	    current-data-quantity data-quantity <> if data-quantity-er throw then
	then
	false
    restore   dup
	if
	    swap drop swap drop    \ errors will be returned
	    free-parsed-data drop  \ this means if memory could not be freed there will be no error detected
	then                       \ this needs to be done to free up memory or leaks will happen 
    endtry ;

0 value iter-nodes
: iter-parsed-data ( -- caddr nflag ) \ will start at the beginning of data node and return structure address
    \ nflag will be false if the caddr is valid
    \ nflag will be true when data not present or done iterating through 
    \ This word will always restart the node after it reaches the end next time called
    parsed-data @ { cur-node } cur-node  
    0 =
    if
	0 true 
    else
	iter-nodes 0 ?do
	    cur-node next-data-node @ to cur-node
	loop
	cur-node next-data-node @ 0 =
	if
	    0 to iter-nodes
	    0 true
	else
	    iter-nodes 1+ to iter-nodes
	    cur-node
	    false
	then
    then ;

: (parsed-data!) ( caddr-table ut -- nflag ) \ form sql query from data nodes and issue to sqlite3 with response of nflag
    try
	setupsqlite3
	s" " dbfieldseparator
	s" " dbrecordseparator
	2dup s" select quantity from devices where data_table = '" temp$ $!
	temp$ $+! s" ';" temp$ $+! temp$ $@ dbcmds
	sendsqlite3cmd dberrorthrow
	dbret$ s>number? true =
	if
	    d>s data-quantity <> if json-quantity-er throw then
	else
	    quantity-retreave-er throw
	then
	setupsqlite3
	s" insert into " temp$ $! temp$ $+! s" (dtime," temp$ $+!
	0 to iter-nodes  \ to ensure starting at begining of data nodes
	begin
	    iter-parsed-data
	    if
		drop true
	    else
		name$ $@ temp$ $+! s" ," temp$ $+!
		false
	    then
	until
	temp$ dup $@len 1- swap $!len \ remove the last , from the string
	s\" \) values\(" temp$ $+!
	datetime$ temp$ $+!
	begin
	    iter-parsed-data
	    if
		drop true
	    else \ data_quantity$ are interger in the database so no ' are needed!
		value$ $@ temp$ $+! s" ," temp$ $+!
		false
	    then
	until
	temp$ dup $@len 1- swap $!len \ remove the last , from string
	s" );" temp$ $+!
	temp$ $@ dbcmds sendsqlite3cmd dberrorthrow
	false
    restore dup if swap drop swap drop then
    endtry ;

: parse-data-table! ( caddr-table ut caddr-data ud -- nflag ) \ caddr-data is a string that needs to be parsed and stored into
    \ database at the table named in the string caddr-table.
    \ nflag is false if data was parsed correctly and data then stored into table of database correctly
    try
	2swap 2dup
	sqlite-table? dup table-no = if datatable-name-er throw else dup table-yes <> if throw else drop then then
	2swap 
	(parse-data-table) throw
	(parsed-data!) throw 
	free-parsed-data throw
	false
    restore dup if >r 2drop 2drop r> then 
    endtry ;

list$: devices$

: registered-devices@ ( -- ) \ puts the current names of devices into devices$ 
    setupsqlite3
    s" " dbrecordseparator
    s" select data_table from devices;" dbcmds sendsqlite3cmd dberrorthrow
    dbret$
    devices$-$off
    begin
	44 $split
	2swap
	devices$-$!
	dup 0 =
    until
    2drop ;

list$: connection$s

: named-device-connection$ ( caddr-dname u -- ) \ from a registered device name produce connection$s strings! 
    setupsqlite3
    connection$s-$off
    s" select ip,port,method from devices where data_table ='" temp$ $!
    temp$ $+! s" ';" temp$ $+! temp$ $@ dbcmds
    sendsqlite3cmd dberrorthrow
    dbret$
    44 $split 2swap connection$s-$! \ ip address
    44 $split 2swap connection$s-$! \ port number in a string from
    44 $split 2drop connection$s-$! \ method string to talk to sensor
    ;

list$: field$s
variable fieldtable$    
: get-table>fields ( caddr-table ut -- nflag )  \ will get the field names of a table.  nflag is false if the field names are valid
    \ field$s will contain the names if nflag is false or will be 
    try
	2dup sqlite-table? dup table-yes =
	if
	    drop 
	    setupsqlite3
	    s" ," dbfieldseparator
	    s"  " dbrecordseparator
	    s" pragma table_info(" fieldtable$ $! fieldtable$ $+! s" );" fieldtable$ $+!
	    fieldtable$ $@ dbcmds
	    sendsqlite3cmd dberrorthrow 
 	    dbret$
	    field$s-$off  \ clear the holder for the field name strings
	    begin
		32 $split 2swap \ break up record separators 
		',' scan ',' skip ',' $split 2drop field$s-$! \ find the second field with the table name and store it
		dup 0= \ am i done
	    until 
	    2drop 
	    false
	else
	    dup table-no = if drop 2drop field->table-err throw else throw then
	then
    restore dup if swap drop swap drop then
    endtry ;

: max(rowid)$ ( caddr-table u -- caddr-max umax ) \ gets the max(rowid) of a sqlite3 named table and returns the string
    setupsqlite3
    s" " dbfieldseparator
    s" " dbrecordseparator
    s" select max(rowid) from " temp$ $! temp$ $+! s" ;" temp$ $+!
    temp$ $@ dbcmds
    sendsqlite3cmd dberrorthrow
    dbret$ ;


\ *******************************************************
\ some tools to look at data
: totalerrors ( -- caddr u nflag )  \ nflag is false if string is valid.  caddr u is current total error count in database in string format 
    try
	setupsqlite3
	s" " dbfieldseparator
	s" " dbrecordseparator
	s" select max(rowid) from errors;" dbcmds \ remember rowid is a speical name
	sendsqlite3cmd dberrorthrow
	dbret$ 
	false
    restore dup if 0 swap 0 swap then 
    endtry ;
    
variable errjnk$
: listerrors ( -- )
    setupsqlite3
    s" select max(rowid) from errorList;" dbcmds  \ remember rowid is a special name
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
    if  cr
	d>s 0 { end now }  begin
	    s" select error,errorText from errorList limit 1 offset " errjnk$ $!
	    now s>d dto$ errjnk$ $+! s" ;" errjnk$ $+!
	    errjnk$ $@ dbcmds
	    sendsqlite3cmd 0<>
	    if
		s" **sql msg**" type dberrmsg drop type
		begin
		    2 ms
		    sendsqlite3cmd 0=
		until
		now 1- to now
	    else
		dbret$ type \ cr
	    then
	    now 1+ to now
	    now end >=
	until
    then ;

: listdberrors ( -- )
    setupsqlite3
    s" select max(rowid) from errors;" dbcmds
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
	    s" select row,datetime(dtime,'unixepoch','localtime'),error from errors limit 1 offset " errjnk$ $!
	    now s>d dto$ errjnk$ $+! s" ;" errjnk$ $+!
	    errjnk$ $@ dbcmds
	    sendsqlite3cmd 0<>
	    if
		s" **sql msg**" type dberrmsg drop type
		begin
		    2 ms
		    sendsqlite3cmd 0=
		until
		now 1- to now
	    else
		dbret$ type \ cr
	    then
	    now 1+ to now
	    now end >=
	until
    then ;

list$: lastrecord$s
: last-record ( caddr-table u -- )
    2dup
    setupsqlite3
    s" ," dbfieldseparator
    s" " dbrecordseparator
    s" select * from " temp$ $! temp$ $+! s"  where row = (select max(rowid) from " temp$ $+! temp$ $+! s" );" temp$ $+!
    temp$ $@ dbcmds
    sendsqlite3cmd dberrorthrow
    dbret$
    lastrecord$s-$off  \ clear the holder for the field name strings
    begin
	',' $split 2swap lastrecord$s-$!
	dup 0= \ am i done
    until
    2drop ;







    




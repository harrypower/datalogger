#! /usr/bin/gforth

warnings off
include ../string.fs
include script.fs

variable junk$
variable mount_name$
s" /mnt/usb0" mount_name$ $!
variable datalogger_path$
variable dbname$
variable dbname-path$

s" /var/lib/datalogger-gforth/datalogger_home_path" slurp-file datalogger_path$ $!
s" /collection/sensordb.data" datalogger_path$ $@ junk$ $! junk$ $+! junk$ $@ dbname$ $!
s" sensordb" dbname-path$ $!

: dto$ ( d -- caddr u )  \ convert double signed to a string
    swap over dabs <<# #s rot sign #> #>> ;

: filetest ( caddr u -- nflag )  \ a file path and name is tested flag is true if file is present
    s" test -e " junk$ $! junk$ $+! s"  && echo 'yes' || echo 'no'" junk$ $+! junk$ $@ shget throw s" yes" search swap drop swap drop
    if true
    else false
    then ;

: checkusb? ( -- nflag ) \  nflag will be true a usb drive mounted and false for no drive mounted !
    s" df -h " shget 0=
    if
	s" usb0" search swap drop swap drop
	if true
	else false
	then
    else
	2drop false
    then ;

: finddev? ( -- nflag ) \ true returned means a usb drive to mount was found and mounted. A false returned means no mounted usb drive!
    s" /dev/sda1" filetest
    if  s" sudo mount /dev/sda1 " junk$ $! mount_name$ $@ junk$ $+! s"  -o nofail,sync,noexec " junk$ $+! junk$ $@ system $? 0=
	if
	    checkusb? 
	    if  true
	    else false
	    then
	else
	    false
	then
    else false
    then ;

: umountdev ( -- nerror ) \ returns true only if the usb drive usb0 has been unmounted with no errors.  Returns false for any errors.
    s" sudo umount " junk$ $! mount_name$ $@ junk$ $+! junk$ $@  system $? 0=
    if  checkusb?
	if  false  \ for some reason drive still there
	else
	    true   \ drive is unmounted.
	then
    else  false
    then ;

: unmount_do ( -- ) \ trys to unmount the usb0 drive 3 times 
    3 0 ?do
	umountdev true = 
	if leave then
    loop ;

: copydb ( --  ) \ copies the db file onto the usb0 that should be mounted.
    s" sudo cp -u " junk$ $! dbname$ $@ junk$ $+! s"  " junk$ $+! mount_name$ $@ junk$ $+! s" /" junk$ $+!
    dbname-path$ $@ junk$ $+! s" ." junk$ $+! 
    time&date s>d dto$ junk$ $+! s>d dto$ junk$ $+! s>d dto$ junk$ $+! 2drop drop
    junk$ $@ system ;

    
: mount&copy ( -- ) \ trys to mount the usb drive then copy db file then unmount drive
    checkusb? false =
    if
	finddev? drop 
    then
    checkusb? true =
    if
	copydb
    then
    \ unmount_do
;

mount&copy bye
#! /usr/local/bin/gforth
\ the above line works on 0.7.3 gforth and up
\ #! /usr/bin/gforth
\ version 0.7.0 has the /local removed from the path to work

\ This Gforth code is a Raspberry Pi usb drive mount and backup for datalogging
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
\
\  This code is run via an external setup cron job.
\  The code will simply mount a usb drive it found and not already mounted at /mnt/usb0.
\  Then the code will backup the current sensor.data file with a datetime stamp on the file name.
\  Note once the drive is mounted it will stay mounted but there is a word that can be uncommented
\  to do the unmounting if needed.

warnings off
require string.fs
require script.fs
require sqlite3-stuff.fs

variable junk$
variable mount_name$
s" /mnt/usb0" mount_name$ $!

variable dbbackupname$

s" datalogged" dbbackupname$ $!

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
    \ makes the file name have extention of yearmonthdayhour
    s" sudo cp -u " junk$ $! db-path$ $@ junk$ $+! s"  " junk$ $+! mount_name$ $@ junk$ $+! s" /" junk$ $+!
    dbbackupname$ $@ junk$ $+! s" ." junk$ $+!
    time&date s>d dto$ junk$ $+! s>d dto$ junk$ $+! s>d dto$ junk$ $+! s>d dto$ junk$ $+! 2drop
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

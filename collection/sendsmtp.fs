#! /usr/bin/gforth

\ This code will send a email if some sensor issue is detected
include ../string.fs
include script.fs

variable junk$
junk$ $init
variable msg-smtp$
s" msg.txt" msg-smtp$  $!  \ the file name for r the smtp send  message
0 value msg-hnd

: filetest ( caddr u -- nflag )  \ looks for the full path and file name in string and returns true if found
    s" test -e " junk$ $! junk$ $+! s"  && echo 'yes' || echo 'no'" junk$ $+! junk$ $@ shget throw 1 -  s" yes" compare  
    if false
    else true
    then ;

: make-msg ( -- ) \ this will make a file called msg.txt 
    msg-smtp$ $@ filetest false =
    if
	msg-smtp$ $@ w/o create-file throw to msg-hnd
    else
	msg-smtp$ $@ w/o open-file throw to msg-hnd
    then
    s" TO: philipkingsmith@gmail.com" msg-hnd write-line throw
    s" From: thpserver@home.com" msg-hnd write-line throw
    s" Subject: Errors on THPserver possible problems!" msg-hnd write-line throw
    s" " msg-hnd write-line throw
    s" The errors were : " msg-hnd write-line throw
    msg-hnd flush-file throw
    msg-hnd close-file throw ;

: send-smtp ( -- )
    s" ssmtp philipkingsmith@gmail.com < msg.txt" system ;


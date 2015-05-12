require cryptobj.fs
require gforth-misc-tools.fs
require script.fs
require stringobj.fs

string heap-new constant shlast$
string heap-new constant passf$
string heap-new constant edata$
string heap-new constant senddata$

\ this next word is needed to prevent system defunct processes when using sh-get from script.fs
: shgets ( caddr u -- caddr1 u1 nflag ) \ like shget but will not produce defunct processes
    TRY   \ nflag is false the addr1 u1 is the result string from the sh command
	\ nflag is true could mean memory was not allocated for this command or sh command failed
	shlast$ !$
	s"  ; echo ' ****'" shlast$ !+$ shlast$ @$ sh-get
	2dup shlast$ !$
	s\"  ****\x0a" search true =
	if
	    swap drop 6 =
	    if
		shlast$ @$ 6 - shlast$ !$ shlast$ @$
	    else      
		shlast$ @$
	    then
	else
	    shlast$ @$
	then $?
    RESTORE
    ENDTRY ;

path$ @$ encrypt_decrypt heap-new value myed

s" stuff to send as test 12345"
path$ @$ passf$ !$ s" testpassphrase" passf$ !+$
passf$ @$ myed ' encrypt$ catch dup 0 = [if] drop edata$ !$ [else] . ."  encryption failed exiting now!" bye [then]

s" curl --data '" senddata$ !$
edata$ @$ senddata$ !+$
s" ' 192.168.0.113:4445/testsend.shtml" senddata$ !+$
senddata$ @$ shgets dup 0 =
[if]
    drop type
[else]
    . ."  some error in the curl statement occured!"
[then]

bye

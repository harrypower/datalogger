#! /usr/bin/gforth

\ This Gforth code part of my library of linux tools!
\    Copyright (C) 2013  Philip King Smith

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

clear-libs
c-library mysemaphore
s" rt" add-lib
\c #include <semaphore.h> 
\c #include <sys/stat.h>
\c #include <fcntl.h>

\c sem_t * info_sem_t(){ return(SEM_FAILED); }
\c int info_oflag(){ return(O_CREAT | O_EXCL); }

c-function sem-info-sem_t info_sem_t -- a
c-function sem-info-oflag info_oflag -- n
c-function sem-open sem_open a n n n -- a
c-function sem-openexisting sem_open a n -- a 
c-function sem-close sem_close a -- n
c-function sem-unlink sem_unlink a -- n
c-function sem-getvalue sem_getvalue a a -- n
c-function sem-dec sem_wait a -- n
c-function sem-inc sem_post a -- n
c-function sem-trydec sem_trywait a -- n

end-c-library

: *char ( caddr u -- caddr ) \ note this clobers pad up to u + 2 elements 
    dup 2 + pad swap erase pad swap move pad  ;

\ This is used to get SEM_FAILED system pointer and the oflag values for use with semaphores.
: semaphore-constants ( -- noflag asem_t* ) \ returns oflag values, mode_t values and SEM_FAILED value for use with semaphores below.
    sem-info-oflag sem-info-sem_t ;   

\ This is to open an existing named semaphore in caddr u.  You get a sem_t* to access semaphore until you close it so save this value to access it!
: open-existing-sema ( caddr u -- asem_t* nflag ) \ nflag is false if semaphore was made and asem_t* is now pointer to semaphore
    *char
    0 sem-openexisting dup
    semaphore-constants swap drop  = ;

\ This is to create a new named semaphore with a starting value.  You will get sem_t* pointer to access the semaphore until you close it so save this value!
: open-named-sema ( caddr u nvalue -- asem_t* nflag ) \ nflag is false if semaphore was made and asem_t* is now pointer to semaphore
    >r *char
    semaphore-constants r> swap >r 436 swap 
    sem-open dup r> = ;

\ This is to access the named semaphore value but you need the sem_t* pointer received during open!
: semaphore@ ( asem_t* -- nvalue nflag ) \ nflag is false if nvalue is valid.  nvalue is semaphore value.  Note pad is clobered. 
    0 pad ! pad sem-getvalue pad @ swap ;

\ This is to close access to semaphore pointed to by asem_t*
: close-semaphore ( asem_t* -- nflag ) \ nflag is false if semaphore was closed
    sem-close ;

\ This is to delete the semaphore from the system.
: remove-semaphore ( caddr u -- nflag ) \ nflag is false if semaphore was removed without errors
    *char sem-unlink ;

\ This is to add one to a semaphores value.
: semaphore+ ( asem_t* -- nflag ) \ nflag is false if semaphore was incremented by one.
    sem-inc ;

\ This is to reduce a semaphores value by one but it blocks so see below for non blocking decrement.
: semaphore- ( asem_t* -- nflag ) \ nflag is false if semaphore was decremented by one.  Note this blocks if semaphore value is zero upon entry.
    sem-dec ;

\ This decrements a semaphores value by one and if that can't happen an error will be returned so it is not blocking.
: semaphore-try- ( asem_t* -- nflag ) \ nflag is false if semaphore was decremented by one. Note this does not block as semaphore- does but will return error if failed to decrement semaphore.
    sem-trydec ;

\ ***************************************************
\ Note the following words are not finished and need some error checking and memory managment issue to be worked on yet!
\ The following words allow treating a semaphore something like a variable in gforth
\ use as follows:
\ 3234 sema-make-named mysemavar .  \ This would make a semaphore named mysemavar that contained 3234 at start.  False returned means all ok semaphore made.
\ mysemavar . . \ you would see 0 3234 returned to command line.
\ sema-close mysemavar . \ would close the sema but the memory for the gforth structure is still present
\ sema-rm mysemavar . \ should remove the semaphore from linux system but the structure is still in gforth

\ Used to make a named semaphore with an initial value of nvalue. nflag is false for all ok.
\ When the "semaphore-name" is used it will return the current semaphore value and a false nflag meaning the value is valid
: sema-make-named ( compilation. nvalue "semaphore-name" -- nflag : run-time. -- ncurrentvalue nflag )
    CREATE latest name>string rot open-named-sema swap , dup , DOES> dup cell + @ if drop 0 true else @ semaphore@ then ;

\ Used to open an existing named semaphore variable to use with words following.  Nflag should be false for opened successfuly!
: sema-open-existing ( compilation. "semaphore-name" -- nflag : run-time. -- ncurrentvalue nflag )
    CREATE latest name>string open-existing-sema swap , dup , DOES> dup cell + @ if drop 0 true else @ semaphore@ then ;

\ Need to use the same name that was used to create or open semaphore as used in sema-make-named or sema-open-existing for all the following words!
\ Use this to increment semaphore. nflag is false for increment happened.
: sema+ ( "semaphore" -- nflag )
    ' >body @ sem-inc ;

\ Use this to decrement semaphore but realize this blocks if value is at zero already!  Nflag is false for decrement worked fine!
: sema- ( "semaphore" -- nflag )
    ' >body @ sem-dec ;

\ Use this to decrement semaphore without blocking.  Nflag is false for decrement worked properly true for can not decrement for some reason.
: sema-try- ( "semaphore" -- nflag )
    ' >body @ sem-trydec ;

\ Use this to close the semaphore.  To open again you use sema-open-existing providing you have not removed the semaphore from system with sema-rm.  False means semaphore closed properly.
: sema-close ( "semaphore" -- nflag )
    ' >body @ sem-close ;

\ Use this to remove the semaphore from the system perminently.  nflag is false if this semaphore does get deleted properly here!
: sema-rm ( "semaphore" -- nflag )
    ' >name dup if dup -2048 <> if name>string *char sem-unlink else drop then else drop then ;
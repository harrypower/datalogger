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


\c int info(sem_t * fail){
\c    fail = SEM_FAILED ;
\c    return( O_CREAT | O_EXCL ) ; }

c-function sema-info info a -- n
c-function sem-open sem_open a n n n -- a
c-function sem-openexisting sem_open a n -- a 
c-function sem-close sem_close a -- n
c-function sem-unlink sem_unlink a -- n
c-function sem-getvalue sem_getvalue a a -- n
c-function sem-dec sem_wait a -- n
c-function sem-inc sem_post a -- n
c-function sem-trydec sem_trywait a -- n

end-c-library

\ variable sem_t*
\ variable sem_failed
\ variable semaphore-value
\ 436 constant root-user-access  \ this is the same as in c 0664 or octal 664 ( root read/write, user read/write, world read )

: *char ( caddr u -- caddr ) \ note this clobers pad up to u elements 
    dup 2 + pad swap erase pad swap move pad  ;

\ : named-sema-open { caddr u nvalue -- nflag } \ nflag is false for named sema created true for named sema failed to create might even already exist!
\    caddr u *char             \ (char * name) name string of semaphore ready to pass to c
\    sem_failed sema-info      \ (int oflag) O_CREAT | O_EXCL value from system ready to pass to c
\    root-user-access          \ (mode_t mode) this is like 0664 and ready to pass to c
\    nvalue semaphore-value !  \ stored the start value to transfer into semaphore 
\    nvalue                    \ now that value is ready to pass to c
\    sem-open                  \ open the new semaphore and see if it works or not
\    sem_t* !                  \ stored the sem_t * pointer to use later
\    sem_t* @ sem_failed @  =  \ compare with failure condition
\ ;                             \ Note sem_t* now contains the address to the semaphore to use to access this named semaphore

\ : semaphore-value ( -- nvalue nflag )  \ nflag will false if the nvalue is valid
\    sem_t* @ semaphore-value sem-getvalue semaphore-value @ swap ;

\ : existing-sema-open ( caddr u -- nflag )  \ nflag is false if existing named semaphore is opened for access
\    *char                           \ (char * name) name string of semaphore ready to pass to c
\    sem_failed sema-info drop       \ getting semaphore failed value 
\    0 sem-openexisting              \ 0 tells sem_open() function to open existing named semaphore
\    sem_t* !                        \ store sem_t* pointer to use later
\    sem_t* @ sem_failed @ =         \ compare with failure condition
\ ;                                   \ Note now sem_t* contains the address to the semaphore for accessing this named semaphore

\ This is used to get SEM_FAILED system pointer and the oflag values for use with semaphores.
: semaphore-constants ( asem_t* -- noflag ) \ this will put SEM_FAILED pointer in asem_t* variable and will return the oflag system values of O_CREAT | O_EXCL
    dup 0 swap ! sema-info ;                \ the asem_t* location is just zeroed before the c function call to ensure i get SEM_FAILED correct value.

\ This is to open an existing named semaphore in caddr u.  You get a sem_t* to access semaphore until you close it so save this value to access it!
: open-existing-sema ( caddr u -- asem_t* nflag ) \ nflag is false if existing named semaphore is opened. For access asem_t* is pointer to semaphore
    *char                               \ (char * name) name string of semaphore ready to pass to c
    0 sem-openexisting dup              \ 0 tells sem_open() function to open existing named semaphore
    pad semaphore-constants drop        \ compare with failure condition
    pad @ =  ;                          \ return (sem_t *) pointer to semaphore and flage  

\ This is to access the named semaphore value but you need the sem_t* pointer received during open!
: semaphore@ ( asem_t* -- nvalue nflag ) \ nflag is false if nvalue is valid.  nvalue is semaphore value.  Note pad is clobered. 
    0 pad ! pad sem-getvalue pad @ swap ;

\ This is to create a new named semaphore with a starting value.  You will get sem_t* pointer to access the semaphore until you close it so save this value! 
: open-named-sema ( caddr u nvalue -- asem_t* nflag ) \ nflag is false if semaphore was opened without errors.  asem_t* is pointer to semaphore. 
    pad semaphore-constants pad @        \ ( caddr u nvalue noflag SEM_FAILED  ) get the oflag constant and SEM_FAILED for use next.  
    { nvalue noflag SEM_FAILED }
    *char noflag 436 nvalue              \ ( char* noflag mode_t) prepare string to pass to c function arrange oflag, mode_t and value.
    sem-open dup                         \ open semaphore
    SEM_FAILED = ;                       \ compare sem_t* returned from sem_open() with SEM_FAILED done!
                                         \ Note the 436 is decimal for 664 in octal.  This is file permissions but the semaphore seems to always
                                         \ have permission only for root read and write.  Not sure how to fix this yet so access the seam via root at this time!

: close-semaphore ( asem_t* -- nflag ) \ nflag is false if semaphore was closed
    sem-close ;

: remove-semaphore ( caddr u -- nflag ) \ nflag is false if semaphore was removed without errors
    *char sem-unlink ;

: semaphore+ ( asem_t* -- nflag ) \ nflag is false if semaphore was incremented by one.
    sem-inc ;

: semaphore- ( asem_t* -- nflag ) \ nflag is false if semaphore was decremented by one.  Note this blocks if semaphore value is zero upon entry.
    sem-dec ;

: semaphore-try- ( asem_t* -- nflag ) \ nflag is false if semaphore was decremented by one. Note this does not block as semaphore- does but will return error if failed to decrement semaphore.
    sem-trydec ;


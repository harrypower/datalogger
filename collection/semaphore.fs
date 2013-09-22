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

: *char ( caddr u -- caddr ) \ note this clobers pad up to u elements 
    dup 2 + pad swap erase pad swap move pad  ;

\ This is used to get SEM_FAILED system pointer and the oflag values for use with semaphores.
: semaphore-constants ( -- noflag asem_t* ) \ returns oflag values, mode_t values and SEM_FAILED value for use with semaphores below.
    sem-info-oflag sem-info-sem_t ;   

\ This is to open an existing named semaphore in caddr u.  You get a sem_t* to access semaphore until you close it so save this value to access it!
: open-existing-sema ( caddr u -- asem_t* nflag )
    *char
    0 sem-openexisting dup
    semaphore-constants swap drop  = ;

\ This is to create a new named semaphore with a starting value.  You will get sem_t* pointer to access the semaphore until you close it so save this value!
: open-named-sema ( caddr u nvalue -- asem_t* nflag )
    >r *char
    semaphore-constants r> swap >r 436 swap 
    sem-open dup r> = ;

\ This is to access the named semaphore value but you need the sem_t* pointer received during open!
: semaphore@ ( asem_t* -- nvalue nflag ) \ nflag is false if nvalue is valid.  nvalue is semaphore value.  Note pad is clobered. 
    0 pad ! pad sem-getvalue pad @ swap ;

\ This is to close the current process's access to the semaphore
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


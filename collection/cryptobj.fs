\ This Gforth code is a encrypt and decrypt object using gpg --symmetric with passphrase
\ to a Beagle bone black server of aggregated data from several sensors

\    Copyright (C) 2015  Philip K. Smith

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

\ This code simply is a front end for GPG encryption program in most debian distros.
\ The encryption and decryption is using symmetric with a passphrase.
\ AES256 is used as cipher algoritom and modification detection code is used!
\ Several files are created in the use of code but removed at the end of each method call.
\ The passphrase is retrieved from a file so it needs to be present to use this object.

require objects.fs
require stringobj.fs
require gforth-misc-tools.fs
require random.fs

object class
    destruction implementation
    inst-value basepath$
    inst-value passphraseF$
    inst-value toencryptF$
    inst-value encrypt_outputF$
    inst-value decrypt_outputF$
    inst-value cmd$
    inst-value rnd$
    cell% inst-var ed_test      \ used to test for constructor execution 
  protected
    m: ( encrypt_decrypt -- ) \ will removes files used in this code
	passphraseF$ @$ filetest true =
	if passphraseF$ @$ delete-file throw then
	toencryptF$ @$ filetest true =
	if toencryptF$ @$ delete-file throw then
	encrypt_outputF$ @$ filetest true =
	if encrypt_outputF$ @$ delete-file throw then
	decrypt_outputF$ @$ filetest true =
	if decrypt_outputF$ @$ delete-file throw then
    ;m method rmfiles
    m: ( -- ) \ create file names to use in encryption and decryption
	\ note the file names have a pattern but old names are destroyed each time this is executed
	utime drop utime drop um* drop seed !
	10000000 random #to$ rnd$ !$
	basepath$ @$ passphraseF$ !$ s" /passphrase" passphraseF$ !+$
	rnd$ @$ passphraseF$ !+$ s" .data" passphraseF$ !+$
	basepath$ @$ toencryptF$ !$ s" /toencrypt" toencryptF$ !+$
	rnd$ @$ toencryptF$ !+$ s" .data" toencryptF$ !+$
	basepath$ @$ encrypt_outputF$ !$ s" /encrypted" encrypt_outputF$ !+$
	rnd$ @$ encrypt_outputF$ !+$ s" .data" encrypt_outputF$ !+$
	basepath$ @$ decrypt_outputF$ !$ s" /decrypted" decrypt_outputF$ !+$
	rnd$ @$ decrypt_outputF$ !+$ s" .data" decrypt_outputF$ !+$
	;m method make-filenames
  public
    m: ( ncaddr u encrypt_decrypt -- )
	\ ncaddr u is string that contains the base path for where files are at and should be placed
	ed_test ed_test @ <>
	if \ constructor run for first time
	    string heap-new [to-inst] basepath$
	    string heap-new [to-inst] passphraseF$
	    string heap-new [to-inst] toencryptF$
	    string heap-new [to-inst] encrypt_outputF$
	    string heap-new [to-inst] decrypt_outputF$
	    string heap-new [to-inst] cmd$
	    string heap-new [to-inst] rnd$
	then
	basepath$ !$
	ed_test ed_test ! \ set constructor test now that constructor has run
    ;m overrides construct
    m: ( encrypt_decrypt -- )
	ed_test ed_test @ =
	if
	    basepath$        dup [bind] string destruct free throw
	    passphraseF$     dup [bind] string destruct free throw
	    toencryptF$      dup [bind] string destruct free throw
	    encrypt_outputF$ dup [bind] string destruct free throw
	    decrypt_outputF$ dup [bind] string destruct free throw
	    cmd$             dup [bind] string destruct free throw
	    rnd$             dup [bind] string destruct free throw
	    0 ed_test ! \ clear construct test because nothing is allocated anymore
	then ;m overrides destruct
  protected
    m: ( ncaddr u -- ) \ ncaddr u a string containing the path and file name for passphrase
	slurp-file     \ note gpg only uses first line for pass phrase
	0 { caddr1 u1 fid }
	this [current] rmfiles
	passphraseF$ @$ w/o create-file throw to fid
	caddr1 u1 fid write-file throw
	fid flush-file throw
	fid close-file throw
    ;m method set_passphrase
  public
    m: ( ncaddr u ncaddr-pass up -- ncaddr1 u1 nflag )
	\ nflag is false for no errors and anything else is an error 
	\ ncaddr u is the string to encrypt ncaddr1 u1 is the returned encrypted string if nflag is false
	\ ncaddr-pass up is string containing the path and name of passphrase file
	try
	    this [current] make-filenames
	    this [current] set_passphrase
	    0 { ncaddr u fid }
	    toencryptF$ @$ w/o create-file throw to fid
	    ncaddr u fid write-file throw
	    fid flush-file throw
	    fid close-file throw
	    
	    s" gpg --passphrase-file " cmd$ !$ passphraseF$ @$ cmd$ !+$
	    s"  --output " cmd$ !+$ encrypt_outputF$ @$ cmd$ !+$
	    s"  --batch --force-mdc --cipher-algo AES256 " cmd$ !+$
	    s"  --symmetric " cmd$ !+$ toencryptF$ @$ cmd$ !+$
	    s"  2> /dev/null" cmd$ !+$ 
	    cmd$ @$ system $? throw
	    encrypt_outputF$ @$ slurp-file
	    this [current] rmfiles
	    false
	restore dup if swap drop swap drop then
	endtry
    ;m method encrypt$
    m: ( ncaddr u ncaddr-pass up -- ncaddr1 u1 nflag )
	\ nflag is false for no errors and anything else is an error
	\ ncaddr u is encrypted string ncaddr1 u1 is decrypted string if nflag is false
	\ ncaddr-pass up is string containing path and name of passphrase file
	try
	    this [current] make-filenames
	    this [current] set_passphrase
	    0 { ncaddr u fid }
	    encrypt_outputF$ @$ r/w create-file throw to fid
	    ncaddr u fid write-file throw
	    fid flush-file throw
	    fid close-file throw 
	    
	    s" gpg --batch --passphrase-file " cmd$ !$ passphraseF$ @$ cmd$ !+$
	    s"  --output " cmd$ !+$ decrypt_outputF$ @$ cmd$ !+$
	    s"  --decrypt " cmd$ !+$ encrypt_outputF$ @$ cmd$ !+$
	    s"  2> /dev/null" cmd$ !+$
	    cmd$ @$ system $? throw
	    decrypt_outputF$ @$ slurp-file
	    this [current] rmfiles
	    false
	restore dup if swap drop swap drop then
	endtry
    ;m method decrypt$
end-class encrypt_decrypt

\ example use below needs a file called testpassphrase with a passphrase in it 
(  
path$ @$ encrypt_decrypt heap-new value tested
s" some crap, to encrypt, as a test!" 2dup dump cr s" testpassphrase"  tested encrypt$ throw 
s" testpassphrase" tested decrypt$ throw dump cr
tested destruct
tested free throw
0 to tested
) 
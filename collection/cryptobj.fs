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

require objects.fs
require stringobj.fs
require gforth-misc-tools.fs

object class
    destruction implementation
    inst-value basepath$
    inst-value passphraseF$
    inst-value toencryptF$
    inst-value encrypt_outputF$
    inst-value decrypt_outputF$
    
    inst-value toencrypt$
    inst-value encrypt_output$
    inst-value decrypt_output$
    inst-value cmd$
    inst-value cmds$
    cell% inst-var ed_test      \ used to test for constructor execution 
    
    m: ( ncaddr u encrypt_decrypt -- )
	\ ncaddr u is the string that contains the base path for where files are at and should be placed
	ed_test ed_test @ <>
	if \ constructor run for first time
	    string heap-new [to-inst] basepath$
	    string heap-new [to-inst] passphraseF$
	    string heap-new [to-inst] toencryptF$
	    string heap-new [to-inst] encrypt_outputF$
	    string heap-new [to-inst] decrypt_outputF$
	    string heap-new [to-inst] toencrypt$
	    string heap-new [to-inst] encrypt_output$
	    string heap-new [to-inst] decrypt_output$
	    string heap-new [to-inst] cmd$
	    string heap-new [to-inst] cmds$
	else \ constructor has run before
	    basepath$ !$
	    basepath$ @$ passphraseF$ !$ s" /passphrase.data" passphraseF$ !+$
	    basepath$ @$ toencryptF$ !$ s" /toencrypt.data" toencryptF$ !+$
	    basepath$ @$ encrypt_outputF$ !$ s" /encrypted.data" encrypt_outputF$ !+$
	    basepath$ @$ decrypt_outputF$ !$ s" /decrypted.data" decrypt_outputF$ !+$
	    toencrypt$ [bind] string construct
	    encrypt_output$ [bind] string construct
	    decrypt_output$ [bind] string construct
	    cmd$ [bind] string construct
	    cmds$ [bind] string construct
	then
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
	    toencrypt$       dup [bind] string destruct free throw
	    encrypt_output$  dup [bind] string destruct free throw
	    decrypt_output$  dup [bind] string destruct free throw
	    cmd$             dup [bind] string destruct free throw
	    cmds$            dup [bind] string destruct free throw
	    0 ed_test ! \ clear construct test because nothing is allocated anymore
	then ;m overrides destruct
end-class encrypt_decrypt


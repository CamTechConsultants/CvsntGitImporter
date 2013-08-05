CvsntGitImporter
================

A CVSNT to Git Importer

# Quick Start #

The quickstart.conf file shows the bare minimum required to import a project
with no branches or tags.

Checkout the CVS source tree to be imported and create a configuration file,
import.conf, to go along with it:

        sandbox   cvstree
        gitdir    project.git
        cvs-cache cache
        debug

        # users
        default-domain example.com
        nobody-name    nobody

Ensure that git is on the path and run

    CvsntGitImporter -C import.conf

This will attempt to import the project checked out in cvstree into Git,
creating a bare repository called project.git.

The following directories are also created:

* cache

  Holds a cache of all the revisions of each file imported, to speed up any
  future import.

* DebugLogs

  Holds a number of debugging log files, including cvs.log, which is a dump of
  all the CVS repository's metadata.

# Rationale #

CVSNT offers a couple of features over vanilla CVS that make importing into
another source control system easier:

1. Commit ids

   When a set of files are committed together they are all tagged with the same
   unique commit id. This makes reconstructing commits easier.

2. Merge points

   When a merge is done, CVSNT records the version on the branch that the merge
   was taken from. This makes importing branches and merges between them
   possible.

I had one large project and a number of other smaller projects that needed
importing from CVSNT into Git. I wanted the history of all the source code to be
available, but I did not need the ability to go back and build older versions
from Git.

This importer has the following features

* Exclude files and directories from being imported

  To keep the size of the Git repository down, it is nice to be able to exclude
  sections of the CVS source tree (in many cases that have been deleted) from
  being imported.

* Import just the head version of some files

  Again, to keep the repository size down, there were portions of the source
  tree where I did not care about the history, e.g. binaries, test data. Files
  can be marked as 'head-only' - the importer ignores these files until the very
  end, when it just imports the current version on the MAIN/master branch and
  on any other branches that are required.

* .cvsignore files converted to .gitignore

* Branches and tags can be renamed via regex patterns

  CVS has quite restrictive rules on what characters can appear in a tag name.
  It is nice to transition to a more friendly naming scheme when importing.

* File revisions can be cached locally to speed up repeated imports

  It is likely in many cases that multiple imports will be made, either to try
  out and debug various options or to do a trial import before doing a final
  import later on, with development occuring in CVS in the meantime. Since
  extracting revisions of files from the CVS server is the dominant part of the
  conversion, caching each revision of each file on disk speeds up subsequent
  imports significantly.

# Details #

## Configuration files ##

CvsntGitImporter can be driven purely by command-line switches, but since there
are quite a number of options, it is better to use a configuration file, some
examples of which are included in the Docs subdirectory. Any command-line
switch, e.g. '--switch arg', becomes a configuration option simply by removing
the leading --, e.g. 'switch arg'.

Comments in the configuration file start with a hash and extend to the end of
the line.

## Commits ##

CVSNT tracks the history of each file separately. However, files committed
together will have the same commit id assigned to them, so it is possible to
construct commits. Earlier versions of CVSNT did not support this feature, so if
there are commits without a commit id, the importer will then attempt to create
commits by matching commit messages that are within a small time window.

## Branches and tags ##

Each branch and tag must be resolved to a single commit. The importer will
attempt to find a commit that represents the state at a particular tag.
Sometimes, this is not immediately possible, particularly when tags have been
moved or made on a tree that is only partially up to date. The importer will
attempt to reorder and split up commits in these cases to find a suitable
commit. However, if the tag is too far away from being a snapshot in time of
the tree, then it will probably fail to be resolved.

Branches can be resolved in a similar way to tags, but it is less reliable,
particularly if files have been added on the branch. In some cases it is not
possible to determine that a file has been added on the branch at a later point
after the branch was made, rather than being there when the branch was made. For
this reason, if possible, use the *branchpoint-rule* option to specify a tag
that points to the point at which the branch was made, assuming that you created
such tags.

**Note, partial tags and branches (i.e. those made on a subset of the CVS tree)
are not supported.**

## Users ##

Git requires e-mail addresses for each user. There are two ways these can be
provided:

1. Define the *default-domain* option

   E-mail addresses will be created by concatenating the CVS username with the
   domain.

2. Supply a users file with the *user-file* option

   This file maps CVS usernames to Git full names and e-mail addresses. The
   format is one user per line, with the CVS username, the Git full name and the
   e-mail address all separated by tabs. If a username is not found in the file,
   then the importer falls back to option 1.

Unlike CVS tags, Git tags are owned by a user, so the importer needs a user to
create tags as. Head-only commits also have no obvious use, so a user is
required here also. In both these cases, the importer uses the *nobody* user. By
default this is called **nobody** with the e-mail address **nobody@** the
*default domain*. The name and e-mail address can be overridden with the
*nobody-name* and *nobody-email* options.

## Speed ##

The slowest part of the import is the extraction of the revisions of each file
from CVS. There are a number of things that can speed the process up:

1. Use the cache

   Subsequent imports will be several orders of magnitude quicker

2. Access the CVS RCS files locally

   Either copy the CVS server files to your local machine or run the importer on
   the server using the :local: protocol.

3. Run several CVS instances in parallel

   Use the *cvs-processes* option to configure the number of CVS processes that
   are run in parallel. Note, that CVS locking within a directory means that
   only one process can be active at any one time within a directory, so the
   maximum number of processes are not always active.

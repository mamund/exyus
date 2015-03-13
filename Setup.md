## Requirements ##
Below are the requirements for successfully setting up this version of **Exyus**:
  * Windows XP, Windows 2003, or Windows Vista (not tested)
  * Internet Information Server 6.0 (IIS) installed
  * [ASP.NET Framework 2.0](http://www.microsoft.com/downloads/details.aspx?FamilyID=0856EACB-4362-4B0D-8EDD-AAB15C5E04F5&displaylang=en)
  * [ISAPI\_Rewrite 2.0](http://www.isapirewrite.com/)

## Installing Exyus ##
Once the prerequisite items are installed, you can move on to setting up **Exyus**. Below is the recommended process:
### Set up the File Space ###
  * Create a folder on disk to hold the **Exyus** web (`c:\exyus\`)
  * Copy the contents of the "exyus\_runtime" zip set into the target directory
  * Locate the Storage folder and right click to get the context menu. Select "Properties"
  * On the "Security" tab, highlight the "Users" group and check ON the "Modify" box.
  * Click "Apply" and "OK" to save your changes and close the dialog.
### Configure IIS ###
For these next steps, use the IIS Management Tool
  * Create a new VDir called "xcs" that points to the target folder (`c:\exyus\`)
  * Open the Properties window for this new VDir and on the Virtual Directory tab, make sure to set this folder  with an "Application Name" (Exyus)
  * Click on the "Configuration" button. On the "Mappings" tab, click the "Add" button. Browse to the ASP.NET 2.0 DLL; enter `.xcs` as the extention; select "All Verbs" radio; check ON "Script Engine"; check OFF "Check that file exists" and press "OK" to save your changes.
  * On the Directory Security Tab, Anonymous Access... section select "Edit" button. When the dialog pops up, make sure only "Anonymous Access" is selected. Do not use Digest, Basic, or Integrated security here. That will be handled by **Exyus**.
  * Press OK to save your changes for the root folder.
  * In the folder list for this VDir, right-click on the "storage" folder and select "Properties" from the context menu.
  * On the "Directory" tab, check ON "Write" radio button and press "OK" to save your changes.
### Edit the Exyus web.config ###
For these steps, open the `web.config` file in the root folder for editing.
  * In the `exyusSecurity` section, update the `systemUser` item to reflect your own **system-wide** username;password
  * Save and close to file
### Edit the Exyus config files ###
a subfolder of the application (config} contains additional configuration files used by **Exyus** to control user access, representation mapping, and URL redirect/rewrites.
  * Open the `auth-users.xml` file to update the list of authorized users
  * Update the first user in the file (that is named "exyus") to the user and password you used in the `systemUser` setting of the `web.config`.
  * Save and close the file.
### Configure ISAP\_Rewrite ###
This version of **Exyus** was built to use ISAPI\_Rewrite 2.0. Once you have installed it, you need to set two rules to handle default **Exyus** requests.
  * Open the `httpd.ini` file in a text editor
  * After entering the following two rules, save and close the file.
```
# start support for exyus server
RewriteRule .*(?:/config/|/documents/|/storage/).* [F,I,O]
RewriteRule (.*)/xcs/([^.?]*)(?:\.xcs)?(\?.*)? $1/xcs/$2.xcs$3 [L,I]
# end exyus rules
```
### Configure Samples ###
To get the samples (included with the exyus\_runtime.zip) to work, you need to make an addition to the `web.config` file.
    * Open the `web.config` at the root
    * in the `htpHandlers` section add the following. Save and close the file.
```
<add verb="*" path="*.samples" type="Exyus.Samples.StaticExamples.samplesRoot,Exyus.Samples"/>
```
### Test the Setup ###
Once you have completed all the steps, open your browser to view the Samples application: (`http://localhost/xcs/samples/`). You should see a short list of test pages that you can view.
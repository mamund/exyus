## Authorization ##
**Exyus** handles authorization via the `config/auth-users.xml` configuration file. This file contains user authentication details (user/password) and a list of URLs along with the access rights the user has for that URL.

Below is an example `config/auth-users.xml` file:
```
<users>
  <user name="user1" password="password1">
    <permission path="/xcs/templates/" methods="*" />
    <permission path="/xcs/data/" methods="*" />
    <permission path="/xcs/documents/" methods="*" />
    <permission path="/xcs/" methods="get,head" />
    <permission path="/xcs/editable/" methods="*" />
  </user>
  <user name="guest" password="">
    <permission path="/xcs/templates/" methods="!" />
    <permission path="/xcs/documents/" methods="!" />
    <permission path="/xcs/data/" methods="!" />
    <permission path="/xcs/" methods="get,head" />
    <permission path="/xcs/editable/" methods="get,head" />
    <permission path="/xcs/postable/" methods="get,head,post" />
  </user>
</users>
```

### Permissions ###
**Exyus** handles authorization by mapping URLs and HTTP Methods for the logged-in user. The URLs in the `auth-users.xml` file correspond to entries in the `auth-urls.xml` file. For each `permission` entry, one or more HTTP methods are added. If a method appears for that `permission` element, the user is granted rights to perform that action at the corresponding URL.

The current list of methods understood by **Exyus** are `GET, HEAD, POST, PUT, DELETE, and OPTION`. In addition, there are two other valid values for the `method` attribute.: 1) `*` (asterisk) which grants rights to all methods for that URL; and 2) `!` (exclamation) which revokes rights for all methods for that URL.
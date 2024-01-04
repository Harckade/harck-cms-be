# About
This project is part of the **Harck CMS by [Harckade](https://www.harckade.com) - A free and opensource serverless content management system**

This repository represents the backend part of the backoffice, powered by [Azure functions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-overview?pivots=programming-language-csharp), and it is meant to be deployed alongside with the Harck CMS FE (frontend).
You can find full API specification on the `swagger` file

> [!WARNING]
> Harckade and Harck CMS team is not associated with any entity that is not listed on [Harckade](https://www.harckade.com) official website nor responsible for any damage/content that those entities may produce.
> Harckade is also not responsible for any abuse of local or global laws or policies that may result from malicious actors that use Harckade's technology.

# Global requirements
0. Microsoft Azure account
1. Setup a Microsoft Entra ID (formerly known as Azure Active Directory) tenant. You can follow this [guide](https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-create-new-tenant)
2. Create an `App registration` as a Single-page app. You can follow this [guide](https://learn.microsoft.com/en-us/entra/external-id/customers/how-to-register-ciam-app?tabs=spa)
3. On the created App registration, create `App roles` following this [guide](https://learn.microsoft.com/en-us/entra/identity-platform/howto-add-app-roles-in-apps), with respective keys and values:
    ```javascript
    "Display name" = "Administrator"
    "Value" = "administrator"
    "Description" = "CMS portal administrator"
    ```
    ```javascript
    "Display name" = "Editor"
    "Value" = "editor"
    "Description" = "CMS portal editor. Can do everything an admin can except add/delete users"
    ```
    ```javascript
    "Display name" = "Viewer"
    "Value" = "viewer"
    "Description" = "CMS portal viewer. Cannot edit - View only"
    ```
4. On the left panel, under the `Manage` section go to the `Authentication` tab and add a Single-page application platform.
    - If this is your development instance you may want to add `http://localhost:3000` as your redirect URI
    - For production website use your static web app URI
    - Under the `Implicit grant and hybrid flows` section make sure that only `Access tokens (used for implicit flows)` is checked.
    - On the `Advanced settings` make sure that you do not allow the usage of `public client flows` by selecting the option `No`. `No` should be highlighted.
5. On the left panel, under the `Manage` section go to the `Certificates & secrets` tab and generate a set of credentials with a desired expiration (You will need them later on the next sections of this guide). You can name it anything you want, for example: "harckade_credentials"
6. On the left panel, under the `Manage` section go to the `Token configuration` tab and add an `optional claim` of `access` type - email (The addressable email for this user, if the user has one)
7. On the left panel, under the `Manage` section go to the `Expose an API` tab and add a new scope:
    - Scope name: api
    - who can consent?: Admins and users
    - Admin consent display name: Harck CMS
    - Admin consent description: Authorize the usage of Microsoft Entra ID on Harck CMS
    - State: Enabled
8. On the left panel, under the `Manage` section go to the `API permissions` tab and add the following permissions, by clicking on the `Add a permission` button:
- select `My API` tab
    - api (Harck CMS)
    - add permission
- Microsoft Graph
    - Delegated permissions:
        1. email
        2. offline_access
        3. openid
        4. profile
        5. User.Read
    - Application permissions:
        1. Application.Read.All
        2. AppRoleAssignment.ReadWrite.All
        3. User.Invite.All
        4. User.Read.All
        5. User.ReadWrite.All
    - Then, click on the `Grant admin consent for "YOUR_APP_REGISTRATION_NAME"`
9. Navigate back to the main Azure portal page and open `Microsoft Entra ID`. Then, on the left panel click on the `Enterprise applications`.
    - You should have, at least, one application for Harck CMS with the same name as your app registration
    - Open it and navigate to the `Users and groups` tab
    - Click on the `Add user/group` button, select your user and then the `administrator` role and hit the `Assign` button
10. From your App registration save the `Directory (tenant) ID` and the `Application (client) ID`

# Backend requirements
1. Amazon Web Services account (Required for newsletter and contact form functionality)
2. Email provider that allows custom domains

# Deploy online
What you need to do before you can procceed with the next steps?
1. Clone this repository
2. Setup a new Github personal access token
3. Make sure you completed all steps from **Global requirements** and your Microsoft Entra ID tenant is properly configured

## Amazon Web Services
Create Amazon SES SMTP (Email) resource - newsletter will be send through this service. Follow official AWS [documentation](https://docs.aws.amazon.com/ses/latest/dg/send-email-smtp.html)
1. Your email provider must allow you to use custom domains - usually this is a paid feature ([Proton](https://proton.me/support/custom-domain), [Google](https://workspace.google.com/products/gmail/), [Outlook](https://www.microsoft.com/en-us/microsoft-365/business/business-email-address)). There are some free options, such as [Zoho Mail](https://www.zoho.com/mail/custom-domain-email.html), that you can find by searching the web.
2. Configure your server DNS with Amazon SES MX and TXT records. Follow this [guide](https://docs.aws.amazon.com/ses/latest/dg/mail-from.html)
3. Setup DMARC DKIM and SPF. Follow this [guide](https://docs.aws.amazon.com/ses/latest/dg/send-email-authentication-dmarc.html)
4. Make sure you configured everything properly by scanning your DNS records. You can use a tool such as [MxToolbox](https://mxtoolbox.com/) to do it.

## Microsoft Azure

### Signal R
Create a new SignalR resource
1. Select your desired subscription. For example, `Pay-As-You-Go`
2. Give any valid name for the resource name
3. For the `Region` select the region that is nearest to **you**
4. For the `Pricing tier`, click on `change` and then select `Free` (it should be more than enough for you to start a blog with a couple of people as administrators/editors, you can always revisit it later and create a more powerfull resource)
5. For the `Service mode` select `Serverless`
6. On the `Networking` tab make sure that the `Public endpoint` option is selected
7. Create the resource


### Function App
Go to [Azure portal](https://portal.azure.com/#home) and create a new `Function App` resource for each Harck CMS function
<details>
    <summary>List of <b>Function Apps</b> you will need to create</summary>
    <ul>
        <li>harck-{project name}-admin</li>
        <li>harck-{project name}-journal</li>
        <li>harck-{project name}-newsletter</li>
        <li>harck-{project name}-private</li>
        <li>harck-{project name}-private-newsletter</li>
        <li>harck-{project name}-pub-art</li>
        <li>harck-{project name}-pub-cnt</li>
        <li>harck-{project name}-pub-files</li>
        <li>harck-{project name}-pub-newsletter</li>
        <li>harck-{project name}-signal</li>
    <ul>
</details>

1. Select your desired subscription. For example, `Pay-As-You-Go`
2. Select a resource group (It can be a good ideia to use a single resource group for all Harckade services)
3. On the `Instance Details` specify any name you want for your function
4. Leave the `code` option selected for the deployment option
5. For the `Runtime stack`, select `.NET`
6. The version should be `8`
7. For the region, select the one that is closer to your costumers or the one that you consider to be economically more viable
8. On the `Operating system` make sure to select `Linux`
9. As for the hosting plan, leave the `Consumption (Serverless)` option selected and click on the `Next: Storage >` button
10. Select thee same `Storage account` for all your harckade function apps
11. Go to `Monitoring` tab and select whether you want to have `Application Insights` enabled or not. They may be useful to debug the service, but keep in mind that the may as well increase your storage costs.
12. Go to the `Review + create` tab and finish the creation

#### Function App configuration
1. Once `Function Apps` are created, open each one of them and download the `publish profile` by navigating to the `Overview` window. You will need this on **GitHub Actions configuration** section
2. Then, navigate to the `configuration` section on the left side menu, click `Advanced edit` and add the respective key-value pairs
    > [!WARNING]
    > Do not delete the configurations that already exist there, just add more
    <details>
        <summary>Configurations for each function</summary>
            <ul>
                    <details>
                        <summary>harck-{project name}-admin</summary>
                        <pre>
                            {
                                "name": "AuthenticationAuthority",
                                "value": "https://login.microsoftonline.com/{your-tenant-id}",
                                "slotSetting": false
                            },
                            {
                                "name": "AuthenticationClientId",
                                "value": "api://{your-app-registration-client-id}",
                                "slotSetting": false
                            },
                            {
                                "name": "ClientId",
                                "value": "{your-app-registration-client-id}",
                                "slotSetting": false
                            },
                            {
                                "name": "ClientSecretValue",
                                "value": "{your-app-registration-client-secret-value}",
                                "slotSetting": false
                            },
                            {
                                "name": "DISPATCH_REPO",
                                "value": "{your-github-repo>/harckade-client}",
                                "slotSetting": false
                            },
                            {
                                "name": "GIT_TOKEN",
                                "value": "{your-github-personal-access-token}",
                                "slotSetting": false
                            },
                            {
                                "name": "ObjectId",
                                "value": "{your-app-registration-object-id}",
                                "slotSetting": false
                            },
                            {
                                "name": "RedirectUrl",
                                "value": "{your-blog(harckade-client)-url}",
                                "slotSetting": false
                            },
                            {
                                "name": "TenantId",
                                "value": "{Microsoft-Entra-Id-tenant-id}",
                                "slotSetting": false
                            }
                            </pre>            
                    </details>
                    <details>
                        <summary>harck-{project name}-journal</summary>
                        <pre>
                        No need to edit
                        </pre>
                    </details>
                    <details>
                        <summary>harck-{project name}-newsletter</summary>
                        <pre>
                        No need to edit
                        </pre>
                    </details>
                    <details>
                        <summary>harck-{project name}-private</summary>
                        <pre>
                        {
                            "name": "AuthenticationAuthority",
                            "value": "https://login.microsoftonline.com/{your-tenant-id}",
                            "slotSetting": false
                        },
                        {
                            "name": "AuthenticationClientId",
                            "value": "api://{your-app-registration-client-id}",
                            "slotSetting": false
                        },
                        {
                            "name": "ClientId",
                            "value": "{your-app-registration-client-id}",
                            "slotSetting": false
                        },
                        {
                            "name": "ClientSecretValue",
                            "value": "{your-app-registration-client-secret-value}",
                            "slotSetting": false
                        },
                        {
                            "name": "DISPATCH_REPO",
                            "value": "{your-github-repo>/harckade-client}",
                            "slotSetting": false
                        },
                        {
                            "name": "GIT_BRANCH",
                            "value": "{branch-that-will-be-deployed-on-harckade-client}",
                            "slotSetting": false
                        },
                        {
                            "name": "GIT_TOKEN",
                            "value": "{your-github-personal-access-token}",
                            "slotSetting": false
                        },
                        {
                            "name": "ObjectId",
                            "value": "{your-app-registration-object-id}",
                            "slotSetting": false
                        },
                        {
                            "name": "RedirectUrl",
                            "value": "{your-blog(harckade-client)-url}",
                            "slotSetting": false
                        },
                        {
                            "name": "TenantId",
                            "value": "{Microsoft-Entra-Id-tenant-id}",
                            "slotSetting": false
                        },
                        </pre>
                    </details>
                    <details>
                        <summary>harck-{project name}-private-newsletter</summary>
                        <pre>
                        {
                            "name": "AuthenticationAuthority",
                            "value": "https://login.microsoftonline.com/{your-tenant-id}",
                            "slotSetting": false
                        },
                        {
                            "name": "AuthenticationClientId",
                            "value": "api://{your-app-registration-client-id}",
                            "slotSetting": false
                        },
                                                {
                            "name": "ClientId",
                            "value": "{your-app-registration-client-id}",
                            "slotSetting": false
                        },
                        {
                            "name": "ClientSecretValue",
                            "value": "{your-app-registration-client-secret-value}",
                            "slotSetting": false
                        },
                        {
                            "name": "ObjectId",
                            "value": "{your-app-registration-object-id}",
                            "slotSetting": false
                        },
                        {
                            "name": "TenantId",
                            "value": "{Microsoft-Entra-Id-tenant-id}",
                            "slotSetting": false
                        },
                        {
                            "name": "RedirectUrl",
                            "value": "{your-blog(harckade-client)-url}",
                            "slotSetting": false
                        },
                        {
                            "name": "DefaultEmailTo",
                            "value": "{email-where-you-will-receive-notifications}",
                            "slotSetting": false
                        },
                        {
                            "name": "EmailFrom",
                            "value": "{email-that-your-subscribers-will-see}",
                            "slotSetting": false
                        },
                        {
                            "name": "EmailHost",
                            "value": "email-smtp.{regiion(e.g.:eu-west-1)}.amazonaws.com",
                            "slotSetting": false
                        },
                        {
                            "name": "ConfigSet",
                            "value": "",
                            "slotSetting": false
                        },
                        {
                            "name": "SmtpPassword",
                            "value": "{aws-ses-password}",
                            "slotSetting": false
                        },
                        {
                            "name": "SmtpPort",
                            "value": "587",
                            "slotSetting": false
                        },
                        {
                            "name": "SmtpUsername",
                            "value": "{aws-ses-smtp-username}",
                            "slotSetting": false
                        }
                        </pre>
                    </details>
                    <details>
                        <summary>harck-{project name}-pub-art</summary>
                        <pre>
                        No need to edit
                        </pre>
                    </details>
                   <details>
                        <summary>harck-{project name}-pub-cnt</summary>
                        <pre>
                         {
                            "name": "DefaultEmailTo",
                            "value": "{email-where-you-will-receive-notifications}",
                            "slotSetting": false
                        },
                        {
                            "name": "EmailFrom",
                            "value": "{email-that-your-subscribers-will-see}",
                            "slotSetting": false
                        },
                        {
                            "name": "EmailHost",
                            "value": "email-smtp.{regiion(e.g.:eu-west-1)}.amazonaws.com",
                            "slotSetting": false
                        },
                        {
                            "name": "ConfigSet",
                            "value": "",
                            "slotSetting": false
                        },
                        {
                            "name": "SmtpPassword",
                            "value": "{aws-ses-password}",
                            "slotSetting": false
                        },
                        {
                            "name": "SmtpPort",
                            "value": "587",
                            "slotSetting": false
                        },
                        {
                            "name": "SmtpUsername",
                            "value": "{aws-ses-smtp-username}",
                            "slotSetting": false
                        }
                        </pre>
                    </details>
                   <details>
                        <summary>harck-{project name}-pub-files</summary>
                        <pre>
                        No need to edit
                        </pre>
                    </details>
                   <details>
                        <summary>harck-{project name}-pub-newsletter</summary>
                        <pre>
                        {
                            "name": "DefaultEmailTo",
                            "value": "{email-where-you-will-receive-notifications}",
                            "slotSetting": false
                        },
                        {
                            "name": "EmailFrom",
                            "value": "{email-that-your-subscribers-will-see}",
                            "slotSetting": false
                        },
                        {
                            "name": "EmailHost",
                            "value": "email-smtp.{regiion(e.g.:eu-west-1)}.amazonaws.com",
                            "slotSetting": false
                        },
                        {
                            "name": "ConfigSet",
                            "value": "",
                            "slotSetting": false
                        },
                        {
                            "name": "SmtpPassword",
                            "value": "{aws-ses-password}",
                            "slotSetting": false
                        },
                        {
                            "name": "SmtpPort",
                            "value": "587",
                            "slotSetting": false
                        },
                        {
                            "name": "SmtpUsername",
                            "value": "{aws-ses-smtp-username}",
                            "slotSetting": false
                        }
                        </pre>
                    </details>
                   <details>
                        <summary>harck-{project name}-signal</summary>
                        <pre>
                        {
                            "name": "AuthenticationAuthority",
                            "value": "https://login.microsoftonline.com/{your-tenant-id}",
                            "slotSetting": false
                        },
                        {
                            "name": "AuthenticationClientId",
                            "value": "api://{your-app-registration-client-id}",
                            "slotSetting": false
                        },
                                                {
                            "name": "ClientId",
                            "value": "{your-app-registration-client-id}",
                            "slotSetting": false
                        },
                        {
                            "name": "ClientSecretValue",
                            "value": "{your-app-registration-client-secret-value}",
                            "slotSetting": false
                        },
                        {
                            "name": "ObjectId",
                            "value": "{your-app-registration-object-id}",
                            "slotSetting": false
                        },
                        {
                            "name": "TenantId",
                            "value": "{Microsoft-Entra-Id-tenant-id}",
                            "slotSetting": false
                        },
                        {
                            "name": "RedirectUrl",
                            "value": "{your-blog(harckade-client)-url}",
                            "slotSetting": false
                        },
                        {
                            "name": "AzureSignalRConnectionString",
                            "value": "{your-signalR-primary-Connection-String}", /* you can find it on SignalR/Keys section */
                            "slotSetting": false
                        }
                        </pre>
                    </details>
            <ul>
        </details>
### API Management service
Create a new `API Management service` resource
1. Search for `API Management services` and then click on `Create` 
2. Select your desired subscription. For example, `Pay-As-You-Go`
3. Give any valid name for the resource name
4. For the `Region` select the region that is nearest to **your users**
5. Provide your `organization name` and `administrator email`
6. Select the `Consumption` tier
7. On the monitoring you can optionally acivate `Application Insights` but keep in mind that it will increase the running cost
8. On `Virtual network` make sure that `None` option is selected for the `connectivity type`
9. Review and create the resource

#### API Management service configuration
##### API Management service configuration - Backend
Open your newly created `API Managed service` and, under the `APIs` section, navigate to the `Backends`
<details>
    <summary>Add a <b>Backend</b> for the following services:</summary>
    <ul>
        <li>harck-{project name}-admin</li>
        <li>harck-{project name}-newsletter</li>
        <li>harck-{project name}-private</li>
        <li>harck-{project name}-private-newsletter</li>
        <li>harck-{project name}-pub-art</li>
        <li>harck-{project name}-pub-cnt</li>
        <li>harck-{project name}-pub-files</li>
        <li>harck-{project name}-pub-newsletter</li>
        <li>harck-{project name}-signal</li>
    <ul>
</details>

1. Provide a valid name for each backend and for the `Type` select `Azure resource` and chose the appropriate `Function App` resource
2. Leave the checkboxes on `Validate certificate chain` and `Validate certificate name` and hit `Create`

##### API Management service configuration - API
While you are on your Harckade's `API Managed service`, navigate to `APIs` and add a new API
1. Click on the `Add API` and select HTTP option
2. Provide a valid `display name` and `name` and then click `create`
3. Select your newly created API and on the `Frontend` open the `OpenAPI specification editor` by clicking on the pencil icon
4. Copy the `url` from `servers` section and save it somewhere as note (you can remove it after the next step)
5. Select all text and replace it with the following code 
    <details>
        <summary>OpenAPI specification JSON <b>(replace the line number 9 with your own URL that you copied on the previous step)</b>:</summary>
        <pre>
            {
            "openapi": "3.0.1",
            "info": {
                "title": "Harckade Backend",
                "description": "",
                "version": "1.0"
            },
            "servers": [{
                "url": "{YOUR_SERVER_URL}"
            }],
            "paths": {
                "/files/{*path}": {
                    "get": {
                        "summary": "DownloadFile",
                        "description": "Download files using public API (without authentication)",
                        "operationId": "downloadfile",
                        "parameters": [{
                            "name": "*path",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": ""
                            }
                        }],
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/users": {
                    "get": {
                        "summary": "ListUsers",
                        "description": "ListUsers",
                        "operationId": "get-listusers",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    },
                    "post": {
                        "summary": "InviteUser",
                        "description": "InviteUser",
                        "operationId": "post-inviteuser",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    },
                    "patch": {
                        "summary": "EditUser",
                        "description": "EditUser",
                        "operationId": "patch-edituser",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/settings": {
                    "post": {
                        "summary": "UpdateSettings",
                        "description": "UpdateSettings",
                        "operationId": "post-updatesettings",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    },
                    "get": {
                        "summary": "GetSettings",
                        "description": "GetSettings",
                        "operationId": "get-getsettings",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/journal": {
                    "get": {
                        "summary": "GetJournal",
                        "description": "GetJournal",
                        "operationId": "get-getjournal",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/users/{userId}": {
                    "delete": {
                        "summary": "DeleteUser",
                        "description": "DeleteUser",
                        "operationId": "delete-deleteuser",
                        "parameters": [{
                            "name": "userId",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": "string"
                            }
                        }],
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/languages": {
                    "get": {
                        "summary": "GetLanguages",
                        "description": "GetLanguages",
                        "operationId": "get-getlanguages",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/languages/default": {
                    "get": {
                        "summary": "GetDefaultLanguage",
                        "description": "GetDefaultLanguage",
                        "operationId": "get-getdefaultlanguage",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/zip-files": {
                    "post": {
                        "summary": "ZipFiles",
                        "description": "ZipFiles",
                        "operationId": "post-zipfiles",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/articles/{articleId}/recover": {
                    "patch": {
                        "summary": "RecoverDeletedArticleById",
                        "description": "RecoverDeletedArticleById",
                        "operationId": "patch-recoverdeletedarticlebyid",
                        "parameters": [{
                            "name": "articleId",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": "string"
                            }
                        }],
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/files/{*path}": {
                    "get": {
                        "summary": "ListFiles",
                        "description": "ListFiles",
                        "operationId": "get-listfiles",
                        "parameters": [{
                            "name": "*path",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": ""
                            }
                        }],
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    },
                    "post": {
                        "summary": "UploadFile",
                        "description": "UploadFile",
                        "operationId": "post-uploadfile",
                        "parameters": [{
                            "name": "*path",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": ""
                            }
                        }],
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    },
                    "delete": {
                        "summary": "DeleteFile",
                        "description": "DeleteFile",
                        "operationId": "delete-deletefile",
                        "parameters": [{
                            "name": "*path",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": ""
                            }
                        }],
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/articles/{articleId}/permanent": {
                    "delete": {
                        "summary": "PermanentlyDeleteArticleById",
                        "description": "PermanentlyDeleteArticleById",
                        "operationId": "delete-permanentlydeletearticlebyid",
                        "parameters": [{
                            "name": "articleId",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": "string"
                            }
                        }],
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/articles/deleted": {
                    "get": {
                        "summary": "ListAllDeletedArticles",
                        "description": "ListAllDeletedArticles",
                        "operationId": "get-listalldeletedarticles",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/articles/{articleId}": {
                    "patch": {
                        "summary": "PublishArticleById",
                        "description": "PublishArticleById",
                        "operationId": "patch-publisharticlebyid",
                        "parameters": [{
                            "name": "articleId",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": "string"
                            }
                        }],
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    },
                    "get": {
                        "summary": "GetArticleById",
                        "description": "GetArticleById",
                        "operationId": "get-getarticlebyid",
                        "parameters": [{
                            "name": "articleId",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": "string"
                            }
                        }],
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    },
                    "delete": {
                        "summary": "DeleteArticleById",
                        "description": "DeleteArticleById",
                        "operationId": "delete-deletearticlebyid",
                        "parameters": [{
                            "name": "articleId",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": "string"
                            }
                        }],
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/articles/{articleId}/{lang}/history/{timestamp}": {
                    "patch": {
                        "summary": "RestoreArticleToBackup",
                        "description": "RestoreArticleToBackup",
                        "operationId": "patch-restorearticletobackup",
                        "parameters": [{
                            "name": "articleId",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": "string"
                            }
                        }, {
                            "name": "lang",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": ""
                            }
                        }, {
                            "name": "timestamp",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": ""
                            }
                        }],
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    },
                    "get": {
                        "summary": "GetBackupArticleContentById",
                        "description": "GetBackupArticleContentById",
                        "operationId": "get-getbackuparticlecontentbyid",
                        "parameters": [{
                            "name": "articleId",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": "string"
                            }
                        }, {
                            "name": "lang",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": ""
                            }
                        }, {
                            "name": "timestamp",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": ""
                            }
                        }],
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/robots.txt": {
                    "get": {
                        "summary": "RobotsTxt",
                        "description": "RobotsTxt",
                        "operationId": "get-robotstxt",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/articles": {
                    "get": {
                        "summary": "ListAllArticles",
                        "description": "ListAllArticles",
                        "operationId": "get-listallarticles",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    },
                    "put": {
                        "summary": "AddUpdateArticle",
                        "description": "AddUpdateArticle",
                        "operationId": "put-addupdatearticle",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/deploy": {
                    "get": {
                        "summary": "LaunchDeployment",
                        "description": "LaunchDeployment",
                        "operationId": "get-launchdeployment",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/articles/{articleId}/content": {
                    "get": {
                        "summary": "GetArticleContentById",
                        "description": "GetArticleContentById",
                        "operationId": "get-getarticlecontentbyid",
                        "parameters": [{
                            "name": "articleId",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": "string"
                            }
                        }],
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/articles/{articleId}/{lang}/history": {
                    "get": {
                        "summary": "GetArticleHistory",
                        "description": "GetArticleHistory",
                        "operationId": "get-getarticlehistory",
                        "parameters": [{
                            "name": "articleId",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": "string"
                            }
                        }, {
                            "name": "lang",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": ""
                            }
                        }],
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/files": {
                    "put": {
                        "summary": "AddFolder",
                        "description": "AddFolder",
                        "operationId": "put-addfolder",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    },
                    "get": {
                        "summary": "ListFilesRoot",
                        "description": "ListFilesRoot",
                        "operationId": "get-listfiles-root",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    },
                    "post": {
                        "summary": "UploadFileRoot",
                        "description": "UploadFileRoot",
                        "operationId": "post-uploadfile-root",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/articles/title/{lang}/{title}": {
                    "get": {
                        "summary": "GetPublishedArticleByTitle",
                        "description": "GetPublishedArticleByTitle",
                        "operationId": "get-getpublishedarticlebytitle",
                        "parameters": [{
                            "name": "lang",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": ""
                            }
                        }, {
                            "name": "title",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": ""
                            }
                        }],
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/articles": {
                    "get": {
                        "summary": "ListArticles",
                        "description": "ListArticles",
                        "operationId": "get-listarticles",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/articles/{articleId}/content": {
                    "get": {
                        "summary": "GetPublishedArticleContentById",
                        "description": "GetPublishedArticleContentById",
                        "operationId": "get-getpublishedarticlecontentbyid",
                        "parameters": [{
                            "name": "articleId",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": "string"
                            }
                        }],
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/articles/title/{lang}/{title}/content": {
                    "get": {
                        "summary": "GetPublishedArticleContentByTitle",
                        "description": "GetPublishedArticleContentByTitle",
                        "operationId": "get-getpublishedarticlecontentbytitle",
                        "parameters": [{
                            "name": "lang",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": ""
                            }
                        }, {
                            "name": "title",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": ""
                            }
                        }],
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/articles/{articleId}": {
                    "get": {
                        "summary": "GetPublishedArticleById",
                        "description": "GetPublishedArticleById",
                        "operationId": "get-getpublishedarticlebyid",
                        "parameters": [{
                            "name": "articleId",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": "string"
                            }
                        }],
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/contact": {
                    "post": {
                        "summary": "SendContactForm",
                        "description": "SendContactForm",
                        "operationId": "post-sendcontactform",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/newsletter/unsubscribe": {
                    "post": {
                        "summary": "UnsubscribeNewsletter",
                        "description": "UnsubscribeNewsletter",
                        "operationId": "post-unsubscribenewsletter",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/newsletter": {
                    "post": {
                        "summary": "SubscribeToNewsletter",
                        "description": "SubscribeToNewsletter",
                        "operationId": "post-subscribetonewsletter",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/newsletter/confirm": {
                    "post": {
                        "summary": "ConfirmNewsletterEmail",
                        "description": "ConfirmNewsletterEmail",
                        "operationId": "post-confirmnewsletteremail",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/newsletters/{newsletterId}/send": {
                    "get": {
                        "summary": "SendNewsletterToQueue",
                        "description": "SendNewsletterToQueue",
                        "operationId": "get-sendnewslettertoqueue",
                        "parameters": [{
                            "name": "newsletterId",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": "string"
                            }
                        }],
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/subscribers/{subscriberId}": {
                    "delete": {
                        "summary": "RemoveSubscriber",
                        "description": "RemoveSubscriber",
                        "operationId": "delete-removesubscriber",
                        "parameters": [{
                            "name": "subscriberId",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": "string"
                            }
                        }],
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/newsletters/{newsletterId}": {
                    "get": {
                        "summary": "GetNewsletterById",
                        "description": "GetNewsletterById",
                        "operationId": "get-getnewsletterbyid",
                        "parameters": [{
                            "name": "newsletterId",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": "string"
                            }
                        }],
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    },
                    "delete": {
                        "summary": "DeleteNewsletterById",
                        "description": "DeleteNewsletterById",
                        "operationId": "delete-deletenewsletterbyid",
                        "parameters": [{
                            "name": "newsletterId",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": "string"
                            }
                        }],
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/subscribers": {
                    "get": {
                        "summary": "ListAllSubscribers",
                        "description": "ListAllSubscribers",
                        "operationId": "get-listallsubscribers",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/newsletters": {
                    "get": {
                        "summary": "ListAllNewsletters",
                        "description": "ListAllNewsletters",
                        "operationId": "get-listallnewsletters",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    },
                    "put": {
                        "summary": "AddUpdateNewsletter",
                        "description": "AddUpdateNewsletter",
                        "operationId": "put-addupdatenewsletter",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/newsletters/{newsletterId}/content": {
                    "get": {
                        "summary": "GetNewsletterContentById",
                        "description": "GetNewsletterContentById",
                        "operationId": "get-getnewslettercontentbyid",
                        "parameters": [{
                            "name": "newsletterId",
                            "in": "path",
                            "required": true,
                            "schema": {
                                "type": "string"
                            }
                        }],
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/subscription-template": {
                    "get": {
                        "summary": "GetNewsletterSubscriptionTemplate",
                        "description": "GetNewsletterSubscriptionTemplate",
                        "operationId": "get-getnewslettersubscriptiontemplate",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    },
                    "put": {
                        "summary": "AddOrUpdateNewsletterSubscriptionTemplate",
                        "description": "AddOrUpdateNewsletterSubscriptionTemplate",
                        "operationId": "put-addorupdatenewslettersubscriptiontemplate",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/subscription-template/content": {
                    "get": {
                        "summary": "GetNewsletterContent",
                        "description": "GetNewsletterContent",
                        "operationId": "get-getnewslettercontent",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/notifications/negotiate": {
                    "get": {
                        "summary": "signalRNegotiate",
                        "description": "signalRNegotiate",
                        "operationId": "get-signalrnegotiate",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    },
                    "post": {
                        "summary": "signalRNegotiate",
                        "description": "signalRNegotiate",
                        "operationId": "post-signalrnegotiate",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                },
                "/cms/notifications/sendMessage": {
                    "post": {
                        "summary": "signalRSendMessage",
                        "description": "signalRSendMessage",
                        "operationId": "post-signalrsendmessage",
                        "responses": {
                            "200": {
                                "description": "null"
                            }
                        }
                    }
                }
            },
            "components": {
                "securitySchemes": {
                    "apiKeyHeader": {
                        "type": "apiKey",
                        "name": "Ocp-Apim-Subscription-Key",
                        "in": "header"
                    },
                    "apiKeyQuery": {
                        "type": "apiKey",
                        "name": "subscription-key",
                        "in": "query"
                    }
                }
            },
            "security": [{
                "apiKeyHeader": []
            }, {
                "apiKeyQuery": []
            }]
            }
        </pre>
    </details>
6. Add **Inbound** and **outbout processing** rules by selecting the API, and then click on the `Policy code editor` which should be located on the `<>` icon under Inbound/Outband processing
    <details>
        <summary>Replace {YOUR_FRONTEND_URL} with your own frontend URL. Optionally, you can add multiple entires (E.g.:http://localhost:3000). Also, do not forget to update {project name}:</summary>
        <pre>
            &lt;policies&gt;
                &lt;inbound&gt;
                    &lt;base />
                    &lt;choose&gt;
                        &lt;when condition="@(context.Request.Url.Path.StartsWith("/files") || context.Request.Url.Path.StartsWith("files"))">
                            &lt;set-backend-service backend-id="harck-{project name}-pub-files" />
                        &lt;/when&gt;
                        &lt;when condition="@(context.Request.Url.Path.StartsWith("/cms/users") || context.Request.Url.Path.StartsWith("/cms/settings") || context.Request.Url.Path.StartsWith("/cms/journal") || context.Request.Url.Path.StartsWith("/languages") || context.Request.Url.Path.StartsWith("cms/users") || context.Request.Url.Path.StartsWith("cms/settings") || context.Request.Url.Path.StartsWith("cms/journal") || context.Request.Url.Path.StartsWith("languages"))">
                            &lt;set-backend-service backend-id="harck-{project name}-admin" />
                        &lt;/when&gt;
                        &lt;when condition="@(context.Request.Url.Path.StartsWith("/cms/zip-files") || context.Request.Url.Path.StartsWith("/cms/articles") || context.Request.Url.Path.StartsWith("/cms/files") || context.Request.Url.Path.StartsWith("/robots") || context.Request.Url.Path.StartsWith("/cms/deploy") || context.Request.Url.Path.StartsWith("cms/zip-files") || context.Request.Url.Path.StartsWith("cms/articles") || context.Request.Url.Path.StartsWith("cms/files") || context.Request.Url.Path.StartsWith("robots") || context.Request.Url.Path.StartsWith("cms/deploy"))">
                            &lt;set-backend-service backend-id="harck-{project name}-private" />
                        &lt;/when&gt;
                        &lt;when condition="@(context.Request.Url.Path.StartsWith("/articles") || context.Request.Url.Path.StartsWith("articles"))">
                            &lt;set-backend-service backend-id="harck-{project name}-pub-art" />
                        &lt;/when&gt;
                        &lt;when condition="@(context.Request.Url.Path.StartsWith("/contact") || context.Request.Url.Path.StartsWith("contact"))">
                            &lt;set-backend-service backend-id="harck-{project name}-pub-cnt" />
                        &lt;/when&gt;
                        &lt;when condition="@(context.Request.Url.Path.StartsWith("/newsletter") || context.Request.Url.Path.StartsWith("newsletter"))">
                            &lt;set-backend-service backend-id="harck-{project name}-pub-newsletter" />
                        &lt;/when&gt;
                        &lt;when condition="@(context.Request.Url.Path.StartsWith("/cms/newsletter") || context.Request.Url.Path.StartsWith("/cms/subscribers") || context.Request.Url.Path.StartsWith("/cms/subscription-template") || context.Request.Url.Path.StartsWith("cms/newsletter") || context.Request.Url.Path.StartsWith("cms/subscribers") || context.Request.Url.Path.StartsWith("cms/subscription-template"))">
                            &lt;set-backend-service backend-id="harck-{project name}-private-newsletter" />
                        &lt;/when&gt;
                        &lt;when condition="@(context.Request.Url.Path.StartsWith("/cms/notifications") || context.Request.Url.Path.StartsWith("cms/notifications"))">
                            &lt;set-backend-service backend-id="harck-{project name}-signal" />
                        &lt;/when&gt;
                        &lt;!-- default condition -->
                        &lt;otherwise&gt;
                            &lt;return-response&gt;
                                &lt;set-status code="404" reason="Not Found" />
                            &lt;/return-response&gt;
                        &lt;/otherwise&gt;
                    &lt;/choose&gt;
                    &lt;cors allow-credentials="true">
                        &lt;allowed-origins&gt;
                            &lt;origin&gt;https://{YOUR_FRONTEND_URL}.azurestaticapps.net&lt;/origin&gt;
                        &lt;/allowed-origins&gt;
                        &lt;allowed-methods&gt;
                            &lt;method&gt;GET&lt;/method&gt;
                            &lt;method&gt;POST&lt;/method&gt;
                            &lt;method&gt;OPTIONS&lt;/method&gt;
                            &lt;method&gt;PUT&lt;/method&gt;
                            &lt;method&gt;PATCH&lt;/method&gt;
                            &lt;method&gt;DELETE&lt;/method&gt;
                        &lt;/allowed-methods&gt;
                        &lt;allowed-headers&gt;
                            &lt;header>*&lt;/header&gt;
                        &lt;/allowed-headers&gt;
                        &lt;expose-headers&gt;
                            &lt;header>*&lt;/header&gt;
                        &lt;/expose-headers&gt;
                    &lt;/cors&gt;
                &lt;/inbound&gt;
                &lt;backend&gt;
                    &lt;base />
                &lt;/backend&gt;
                &lt;outbound&gt;
                    &lt;base />
                    &lt;set-header name="Cache-Control" exists-action="override">
                        &lt;value>@{
                            return context.Response.Headers.GetValueOrDefault("Cache-Control", "");
                        }&lt;/value&gt;
                    &lt;/set-header&gt;
                    &lt;set-header name="Date" exists-action="override">
                        &lt;value>@{
                            return context.Response.Headers.GetValueOrDefault("Date", "");
                        }&lt;/value&gt;
                    &lt;/set-header&gt;
                    &lt;set-header name="Expires" exists-action="override">
                        &lt;value>@{
                            return context.Response.Headers.GetValueOrDefault("Expires", "");
                        }&lt;/value&gt;
                    &lt;/set-header&gt;
                    &lt;set-header name="Request-Context" exists-action="override">
                        &lt;value>@{
                            return context.Response.Headers.GetValueOrDefault("Request-Context", "");
                        }&lt;/value&gt;
                    &lt;/set-header&gt;
                    &lt;set-header name="Server" exists-action="override">
                        &lt;value>@{
                            return "Harckade";
                        }&lt;/value&gt;
                    &lt;/set-header&gt;
                    &lt;set-header name="Content-Type" exists-action="override">
                        &lt;value>@{
                            return context.Response.Headers.GetValueOrDefault("Content-Type", "");
                        }&lt;/value&gt;
                    &lt;/set-header&gt;
                    &lt;!-- Retry Policy -->
                    &lt;retry condition="@(context.Response.StatusCode == 503 || context.Response.StatusCode == 500)" count="3" interval="10" max-interval="30" delta="1" first-fast-retry="true">
                        &lt;set-header name="Retry-After" exists-action="override">
                            &lt;value&gt;10&lt;/value&gt;
                        &lt;/set-header&gt;
                    &lt;/retry&gt;
                &lt;/outbound&gt;
                &lt;on-error&gt;
                    &lt;base />
                &lt;/on-error&gt;
            &lt;/policies&gt;
        </pre>
    </details>

##### API Management service configuration - Custom domains (optional)
To setup a custom domain, navigate to the `Custom domains` tab and add your own domain


## GitHub Actions configuration
For this configuration you will need the `Publishing profiles` from the `Function App configuration - step 1`. Once you have them, open the repository you have cloned and navigate to `Settings` section.

There, on the left side expand the `Secrets and variables` under `Security` section and click on `Actions`.
You need to configure the following secrets (make sure the secrets names are spelled correctly, as they are used by GitHub Actions workflow):
1. PUBLISH_ADMIN
2. PUBLISH_ARTICLES
3. PUBLISH_CONTACT
4. PUBLISH_FILES
5. PUBLISH_JOURNAL
6. PUBLISH_NEWSLETTER
7. PUBLISH_PRIVATE
8. PUBLISH_PRIVATE_NEWSLETTER
9. PUBLISH_PUBLIC_NEWSLETTER
10. PUBLISH_SIGNALR

Congratulations! You backend should be fully operational!


# Run locally
If you want to debug this project locally you can use any C# and .NET compatible IDE. On this guide, the focus will be on the Microsoft Visual Studio.


## Requirements
1. Microsoft Visual Studio
2. [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio)


## Guide
Load the project, and add the following file for the function that you want to debug:
<details>
    <summary>local.settings.json</summary>
    <pre>
        {
            "IsEncrypted": false,
            "Values": {
                "AzureWebJobsStorage": "{storage-connection-string}",
                "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
                "AuthenticationAuthority": "https://login.microsoftonline.com/{your-Microsoft-Entra-Id-tenant-id}",
                "AuthenticationClientId": "api://{your-app-registration-client-id}",
                "ClientId": "{your-app-registration-client-id}",
                "ObjectId": "{your-app-registration-object-id}",
                "TenantId": "{your-Microsoft-Entra-Id-tenant-id}",
                "ClientSecretValue": "{your-app-registration-client-secret-value}",
                "RedirectUrl": "http://localhost:3000",
                "DISPATCH_REPO": "{your-github-repo>/harckade-client}",
                "GIT_TOKEN": "{your-github-personal-access-token}",
                "GIT_BRANCH": "{branch-that-will-be-deployed-on-harckade-client}",
                "SmtpPassword": "{aws-ses-password}",
                "SmtpPort": "587",
                "SmtpUsername": "{aws-ses-smtp-username}",
                "EmailFrom": "{email-that-your-subscribers-will-see}",
                "EmailHost": "email-smtp.{regiion(e.g.:eu-west-1)}.amazonaws.com"
            },
            "Host": {
                "LocalHttpPort": 7071,
                "CORS": "http://localhost:3000",
                "CORSCredentials": true
            }
        }
    </pre>
</details>


> [!NOTE]
> If you want to run multiple functions simulatenously make sure that the `LocalHttpPort` value is unique for each function. For example (7071, 7072, 7073, etc.).


Then select the Function you want to test, for example `Harckade.CMS.PublicController.Files`, click on it with the right mouse button, navigate to the `Debug` option and click on the `Start new Instance`

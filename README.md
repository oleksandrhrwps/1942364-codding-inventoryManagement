## Inventory Management System

### Environment:
.NET 8

### Read-Only Files:
InventoryManagement.Tests/IntegrationTests.cs

### Requirements:
The organization is working on inventory management in a warehouse.
The task is to build a basic inventory management system for a warehouse, focusing on item validation, keeping track of their storage location, and identifying discrepancies during the inventory inspections.

**1. CSV Upload and Validation Data**
First of all, the organization is uploading an existing list of items, for further inventory inspections.
Implement a POST endpoint to upload and process inventory data in CSV format with fields:

* _Barcode (GUID, required): Must be a valid GUID_
* _Storage Location Name (string, required)_
* _Item Description (string, optional)_
* _Created Date (datetime, required): Must be a valid date not in the future_

Save valid records to an in-memory storage or database.
Insert only new records based on the Barcode to avoid duplicates.
Return Status 200 (OK) for successful uploads
Return Status 400 (BadRequest) for invalid data with pertinent error messages.

**2. Inventory Process and Item Lookup**
Then, the inventory inspector is performing the inventory process by scanning each item in a warehouse, one by one, in order either to approve the presence of the item in a current location or to find a discrepancy between data specified in the database and actual scanned values.
Implement an endpoint to verify item presence:
Inputs:

* _Barcode (GUID, required)_
* _Storage Location Name (string, required): current storage location where the inventory inspection is going on at the moment_

Return Status 200 (OK) if the item is present at the specified location.
Return Status 404 (NotFound) if the item does not exist in the database at all.
Return Status 400 (BadRequest) if there's a location discrepancy, log the discrepancy with Item GUID, scanning date-time and actual storage location where the discrepancy has been found (use in-memory storage or database for writing).

**3. Discrepancy Reporting**
Implement a GET endpoint to retrieve a list of discrepancies.
Support optional filters for retrieval by scanning date and storage location.
Return Status 200 (OK) for successful retrieval.

**4. Measurements and error handling**
Create a custom middleware that tracks all incoming HTTP requests and responses. The middleware should log the following details to a text file:

* _Request path, body (and headers, if exist)_
* _Response status code (and headers if exist)_
* _Total time taken to process the request_

Include error handling to ensure no requests fail due to logging issues.

import { HttpErrorResponse } from '@angular/common/http';

export function friendly(error: any) {
  let result = '';

  if (error instanceof HttpErrorResponse) {
    const response = <HttpErrorResponse>error;
    switch (response.status) {
      case 0: { // Offline
        result = `Unable to reach the server, please check the connection of your device`;
        break;
      }
      case 400: { // Bad Request
        result = error.error;
        break;
      }
      case 401: { // Unauthorized
        result = `Your login session has expired, please login again`;
        break;
      }
      case 403: { // Forbidden
        result = `Sorry, your account does not have sufficient permissions to view this record`;
        break;
      }
      case 404: { // Not found
        result = `Sorry, the requested resource was not found`;
        break;
      }
      case 500: { // Internal Server Error
        result = `An unhandled exception occurred on the server, please contact your IT department`;
        break;
      }
      default: { // Any other HTTP error
        result = `An unknown error has occurred while retrieving the record, please contact your IT department`;
        break;
      }
    }
  } else {
    console.error(error);
    result = `Unknown error`;
  }

  return result;
}
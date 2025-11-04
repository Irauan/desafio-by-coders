import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TransactionService, TransactionImportOkResponse, TransactionImportErrorResponse, TransactionImportMultiStatusResponse } from './transaction.service';

describe('TransactionService', () => {
  let service: TransactionService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [TransactionService]
    });
    service = TestBed.inject(TransactionService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('importFile', () => {
    let mockFile: File;

    beforeEach(() => {
      mockFile = new File(['test content'], 'test.cnab', { type: 'text/plain' });
    });

    it('should send POST request with FormData', () => {
      const mockResponse: TransactionImportOkResponse = {
        status: 200,
        totalImportedLines: 10,
        importedSummaryPerStores: [
          { storeName: 'Store A', imported: 6 },
          { storeName: 'Store B', imported: 4 }
        ]
      };

      service.importFile(mockFile).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne('/api/v1/transactions/import');
      expect(req.request.method).toBe('POST');
      expect(req.request.body instanceof FormData).toBe(true);

      // Verify FormData contains the file
      const formData = req.request.body as FormData;
      const fileInFormData = formData.get('file');
      expect(fileInFormData).toBeTruthy();
      expect(fileInFormData instanceof File).toBe(true);
      expect((fileInFormData as File).name).toBe(mockFile.name);

      req.flush(mockResponse);
    });

    it('should handle successful import with OK response', () => {
      const mockResponse: TransactionImportOkResponse = {
        status: 200,
        totalImportedLines: 5,
        importedSummaryPerStores: [
          { storeName: 'Store A', imported: 5 }
        ]
      };

      service.importFile(mockFile).subscribe(response => {
        const okResponse = response as TransactionImportOkResponse;
        expect(okResponse.status).toBe(200);
        expect(okResponse.totalImportedLines).toBe(5);
        expect(okResponse.importedSummaryPerStores.length).toBe(1);
      });

      const req = httpMock.expectOne('/api/v1/transactions/import');
      req.flush(mockResponse);
    });

    it('should handle error response with validation errors', () => {
      const mockResponse: TransactionImportErrorResponse = {
        status: 422,
        totalInvalidLines: 3,
        errors: [
          { code: 'CNAB_INVALID_LENGTH', message: 'Invalid line length' },
          { code: 'CNAB_INVALID_TYPE', message: 'Invalid transaction type' }
        ]
      };

      service.importFile(mockFile).subscribe(response => {
        const errorResponse = response as TransactionImportErrorResponse;
        expect(errorResponse.status).toBe(422);
        expect(errorResponse.totalInvalidLines).toBe(3);
        expect(errorResponse.errors.length).toBe(2);
        expect(errorResponse.errors[0].code).toBe('CNAB_INVALID_LENGTH');
      });

      const req = httpMock.expectOne('/api/v1/transactions/import');
      req.flush(mockResponse);
    });

    it('should handle multi-status response', () => {
      const mockResponse: TransactionImportMultiStatusResponse = {
        results: [
          {
            status: 200,
            totalImportedLines: 7,
            importedSummaryPerStores: [{ storeName: 'Store A', imported: 7 }]
          } as TransactionImportOkResponse,
          {
            status: 422,
            totalInvalidLines: 2,
            errors: [{ code: 'CNAB_INVALID_LENGTH', message: 'Invalid length' }]
          } as TransactionImportErrorResponse
        ]
      };

      service.importFile(mockFile).subscribe(response => {
        const multiResponse = response as TransactionImportMultiStatusResponse;
        expect(multiResponse.results).toBeDefined();
        expect(multiResponse.results.length).toBe(2);
      });

      const req = httpMock.expectOne('/api/v1/transactions/import');
      req.flush(mockResponse);
    });

    it('should handle HTTP error (500)', () => {
      const errorMessage = 'Internal server error';

      service.importFile(mockFile).subscribe({
        next: () => fail('should have failed with 500 error'),
        error: (error) => {
          expect(error.status).toBe(500);
          expect(error.statusText).toBe('Server Error');
        }
      });

      const req = httpMock.expectOne('/api/v1/transactions/import');
      req.flush(errorMessage, { status: 500, statusText: 'Server Error' });
    });

    it('should handle network error', () => {
      service.importFile(mockFile).subscribe({
        next: () => fail('should have failed with network error'),
        error: (error) => {
          expect(error.error.type).toBe('error');
        }
      });

      const req = httpMock.expectOne('/api/v1/transactions/import');
      req.error(new ProgressEvent('error'));
    });

    it('should preserve file name in FormData', () => {
      const fileWithName = new File(['content'], 'my-file.cnab', { type: 'text/plain' });

      service.importFile(fileWithName).subscribe();

      const req = httpMock.expectOne('/api/v1/transactions/import');
      const formData = req.request.body as FormData;
      const fileInFormData = formData.get('file') as File;

      expect(fileInFormData.name).toBe('my-file.cnab');

      req.flush({ status: 200, totalImportedLines: 0, importedSummaryPerStores: [] });
    });
  });
});

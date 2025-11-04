import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TransactionImportComponent } from './transaction-import.component';
import { TransactionService, TransactionImportOkResponse, TransactionImportErrorResponse, TransactionImportMultiStatusResponse } from '../transaction.service';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { CommonModule } from '@angular/common';
import { StoresModule } from '../../stores/stores.module';
import { of, throwError } from 'rxjs';

describe('TransactionImportComponent', () => {
  let component: TransactionImportComponent;
  let fixture: ComponentFixture<TransactionImportComponent>;
  let transactionService: jasmine.SpyObj<TransactionService>;

  beforeEach(async () => {
    const transactionServiceSpy = jasmine.createSpyObj('TransactionService', ['importFile']);

    await TestBed.configureTestingModule({
      declarations: [TransactionImportComponent],
      imports: [CommonModule, HttpClientTestingModule, StoresModule],
      providers: [
        { provide: TransactionService, useValue: transactionServiceSpy }
      ]
    }).compileComponents();

    transactionService = TestBed.inject(TransactionService) as jasmine.SpyObj<TransactionService>;
    fixture = TestBed.createComponent(TransactionImportComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Initial State', () => {
    it('should have correct initial values', () => {
      expect(component.fileToUpload).toBeNull();
      expect(component.isUploading).toBe(false);
      expect(component.hasCompletedImport).toBe(false);
      expect(component.importSummary).toBeNull();
      expect(component.validationErrors).toEqual([]);
    });
  });

  describe('onFileChange', () => {
    it('should set fileToUpload when file is selected', () => {
      const mockFile = new File(['test'], 'test.cnab', { type: 'text/plain' });
      const event = {
        target: {
          files: [mockFile]
        }
      } as any;

      component.onFileChange(event);

      expect(component.fileToUpload).toBe(mockFile);
    });

    it('should set fileToUpload to null when no file is selected', () => {
      component.fileToUpload = new File(['test'], 'test.cnab');
      const event = {
        target: {
          files: []
        }
      } as any;

      component.onFileChange(event);

      expect(component.fileToUpload).toBeNull();
    });

    it('should handle null input target', () => {
      const event = {
        target: null
      } as any;

      expect(() => component.onFileChange(event)).not.toThrow();
      expect(component.fileToUpload).toBeNull();
    });
  });

  describe('onImport', () => {
    let mockFile: File;

    beforeEach(() => {
      mockFile = new File(['test content'], 'test.cnab', { type: 'text/plain' });
    });

    it('should not import when no file is selected', () => {
      component.fileToUpload = null;

      component.onImport();

      expect(transactionService.importFile).not.toHaveBeenCalled();
    });

    it('should not import when already uploading', () => {
      component.fileToUpload = mockFile;
      component.isUploading = true;

      component.onImport();

      expect(transactionService.importFile).not.toHaveBeenCalled();
    });

    it('should set correct state when starting import', () => {
      component.fileToUpload = mockFile;
      transactionService.importFile.and.returnValue(of({
        status: 200,
        totalImportedLines: 0,
        importedSummaryPerStores: []
      }));

      component.onImport();

      expect(component.isUploading).toBe(false); // Set to false in complete
      expect(component.hasCompletedImport).toBe(true);
      expect(transactionService.importFile).toHaveBeenCalledWith(mockFile);
    });

    it('should handle successful import with OK response', () => {
      const mockResponse: TransactionImportOkResponse = {
        status: 200,
        totalImportedLines: 10,
        importedSummaryPerStores: [
          { storeName: 'Store A', imported: 6 },
          { storeName: 'Store B', imported: 4 }
        ]
      };

      component.fileToUpload = mockFile;
      transactionService.importFile.and.returnValue(of(mockResponse));

      component.onImport();

      expect(component.importSummary).toEqual(mockResponse);
      expect(component.validationErrors).toEqual([]);
      expect(component.hasCompletedImport).toBe(true);
      expect(component.isUploading).toBe(false);
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

      component.fileToUpload = mockFile;
      transactionService.importFile.and.returnValue(of(mockResponse));

      component.onImport();

      expect(component.importSummary).toBeNull();
      expect(component.validationErrors).toEqual(mockResponse.errors);
      expect(component.hasCompletedImport).toBe(true);
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

      component.fileToUpload = mockFile;
      transactionService.importFile.and.returnValue(of(mockResponse));

      component.onImport();

      expect(component.importSummary).toBeDefined();
      expect(component.importSummary?.totalImportedLines).toBe(7);
      expect(component.validationErrors.length).toBe(1);
      expect(component.hasCompletedImport).toBe(true);
    });

    it('should handle HTTP error', () => {
      const errorResponse = { status: 500, statusText: 'Server Error' };
      component.fileToUpload = mockFile;
      transactionService.importFile.and.returnValue(throwError(() => errorResponse));

      spyOn(console, 'error');

      component.onImport();

      expect(console.error).toHaveBeenCalledWith(errorResponse);
      expect(component.isUploading).toBe(false);
    });

    it('should reset state before importing', () => {
      component.fileToUpload = mockFile;
      component.hasCompletedImport = true;
      component.importSummary = {
        status: 200,
        totalImportedLines: 5,
        importedSummaryPerStores: []
      };
      component.validationErrors = [{ code: 'TEST', message: 'Test error' }];

      transactionService.importFile.and.returnValue(of({
        status: 200,
        totalImportedLines: 0,
        importedSummaryPerStores: []
      }));

      component.onImport();

      // State should be reset during import
      expect(component.validationErrors).toEqual([]);
    });
  });

  describe('parseImportResponse', () => {
    it('should parse OK response correctly', () => {
      const okResponse: TransactionImportOkResponse = {
        status: 200,
        totalImportedLines: 10,
        importedSummaryPerStores: [{ storeName: 'Store A', imported: 10 }]
      };

      const result = (component as any).parseImportResponse(okResponse);

      expect(result.okResponse).toEqual(okResponse);
      expect(result.errors).toEqual([]);
    });

    it('should parse error response correctly', () => {
      const errorResponse: TransactionImportErrorResponse = {
        status: 422,
        totalInvalidLines: 5,
        errors: [
          { code: 'ERROR1', message: 'Error 1' },
          { code: 'ERROR2', message: 'Error 2' }
        ]
      };

      const result = (component as any).parseImportResponse(errorResponse);

      expect(result.okResponse).toBeNull();
      expect(result.errors).toEqual(errorResponse.errors);
    });

    it('should parse multi-status response with both ok and error', () => {
      const multiResponse: TransactionImportMultiStatusResponse = {
        results: [
          {
            status: 200,
            totalImportedLines: 8,
            importedSummaryPerStores: [{ storeName: 'Store A', imported: 8 }]
          } as TransactionImportOkResponse,
          {
            status: 422,
            totalInvalidLines: 2,
            errors: [{ code: 'ERR', message: 'Error message' }]
          } as TransactionImportErrorResponse
        ]
      };

      const result = (component as any).parseImportResponse(multiResponse);

      expect(result.okResponse).toBeDefined();
      expect(result.okResponse?.totalImportedLines).toBe(8);
      expect(result.errors.length).toBe(1);
      expect(result.errors[0].code).toBe('ERR');
    });

    it('should handle error response without errors array', () => {
      const errorResponse: TransactionImportErrorResponse = {
        status: 422,
        totalInvalidLines: 0,
        errors: undefined as any
      };

      const result = (component as any).parseImportResponse(errorResponse);

      expect(result.okResponse).toBeNull();
      expect(result.errors).toEqual([]);
    });
  });

  describe('Template Integration', () => {
    it('should display upload section when import not completed', () => {
      component.hasCompletedImport = false;
      fixture.detectChanges();

      const uploadSection = fixture.nativeElement.querySelector('.upload-section');
      expect(uploadSection).toBeTruthy();
    });

    it('should display results section when import is completed', () => {
      component.hasCompletedImport = true;
      component.importSummary = {
        status: 200,
        totalImportedLines: 10,
        importedSummaryPerStores: []
      };
      fixture.detectChanges();

      const resultsSection = fixture.nativeElement.querySelector('.results');
      expect(resultsSection).toBeTruthy();
    });

    it('should display loading indicator when uploading', () => {
      component.isUploading = true;
      fixture.detectChanges();

      const loading = fixture.nativeElement.querySelector('.loading');
      expect(loading).toBeTruthy();
      expect(loading.textContent).toContain('Processing CNAB file...');
    });

    it('should disable import button when no file selected', () => {
      component.fileToUpload = null;
      component.isUploading = false;
      fixture.detectChanges();

      const importButton = fixture.nativeElement.querySelector('.import-button');
      expect(importButton.disabled).toBe(true);
    });

    it('should enable import button when file is selected', () => {
      component.fileToUpload = new File(['test'], 'test.cnab');
      component.isUploading = false;
      fixture.detectChanges();

      const importButton = fixture.nativeElement.querySelector('.import-button');
      expect(importButton.disabled).toBe(false);
    });
  });
});

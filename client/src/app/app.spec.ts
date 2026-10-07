import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render the Normora title during auth initialisation', async () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;

    // Force the authInitializing signal to true so the loading screen branch renders.
    // Without this the @if block is false and the h1 is not mounted in the DOM.
    (app as any).authInitializing.set(true);

    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent?.trim()).toContain('Normora');
  });
});

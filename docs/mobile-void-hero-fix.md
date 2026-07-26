# Mobile VOID / editorial panel fix

This patch fixes the mobile rendering problem visible in the VOID look panel:

- removes the permanent desktop hover reveal circle on phones
- makes the reveal appear only while tapping
- limits image panel height with `svh`
- prevents oversized `VOID`/drop titles from being cut off
- removes the desktop stagger/drop-number behavior on phones
- keeps desktop styles unchanged

Changed files:

- `src/Tatakae.Web/wwwroot/css/mobile-rescue.css`
- `src/Tatakae.Web/wwwroot/js/tatakae-ink.js`

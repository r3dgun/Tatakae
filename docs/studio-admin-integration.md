# Kimi Studio + Admin Integration

This revision converts the studio from a cosmetic screen into a persistent commerce workflow.

## Studio payload

`EmbroideryCustomizationRequest` now stores the same interaction model visible in the supplied Kimi HTML:

- Garment type: `TShirt`, `Hoodie`, `Sweatshirt`, `Crewneck`
- Garment size and garment color
- Embroidery placement
- Embroidery dimensions
- Thread colors
- Design source: ready motif, uploaded artwork, or text
- Motif key: `dragon`, `sword`, `cloud`, `custom`
- Drag position: `PositionX`, `PositionY`
- Transform controls: `ScalePercent`, `RotationDegrees`, `OpacityPercent`
- Customer note

## Order persistence

The selected studio state is copied into the cart line and then persisted into `OrderLine.Embroidery` during checkout. The API validates the request again before accepting the order.

## Admin workflow

Admin order detail now shows the production data required by embroidery operators:

- selected motif / uploaded file / text
- placement and dimensions
- thread colors
- garment type and color
- X/Y position, scale, rotation and opacity
- customer note and uploaded artwork link

This keeps the Kimi visual language while making the studio usable as a real ecommerce order source.

# Request interface review brief

**Planning only.** Use fictional titles, users, and request states in the design review. Schedule live installation and account configuration for an authorized setup window.

## Starting point

After the download pipeline is verified, review stock Seerr on the Windows server at `127.0.0.1:5055`. Start with the official screenshot of the default request interface, then compare it to the installed version on phone and desktop. [Stock Seerr screenshots](https://seerr.dev/) · [Windows setup](https://docs.seerr.dev/getting-started/buildfromsource/#windows)

On both phone and desktop, review these paths with synthetic examples:

1. Find a title by search, distinguish available from unavailable, and open its details.
2. Request a movie or selected TV seasons; see a clear pending confirmation and request status.
3. As owner, review a pending family request and approve or decline it. Give family accounts permission to request titles and reserve approvals and request management for the owner. Show when a request is approved and when the file is available. [Request statuses and approval permissions](https://docs.seerr.dev/api/update-a-requests-status/)
4. Keep Travel Movies an owner-only, opt-in workflow in the separate Radarr instance. Verify that family requests use the standard movie or TV path.

Check legibility, touch targets, keyboard access, feedback for empty/error states, and whether a person can complete each task without guidance. Record specific friction and the device used.

## Family access choices

- **Text:** simplest fallback; the owner handles requests manually.
- **Plex Watchlist:** optional pilot in the familiar Plex app. Seerr's [Watchlist auto-request documentation](https://docs.seerr.dev/using-seerr/plex/watchlist-auto-request/) requires each user to sign in to Seerr with Plex, receive the relevant auto-request permissions, and enable the feature in their own profile. Requests are checked periodically; leave auto-approve permissions off and verify pending approval. Local users cannot use this feature.
- **Private request page:** optional invite-only Tailscale Serve after testing coexistence with Mullvad. Configure restrictive access and test from an invited and an uninvited device. [Serve](https://tailscale.com/docs/features/tailscale-serve) exposes a local service to authorized tailnet users; [Tailscale cautions](https://tailscale.com/docs/reference/faq/other-vpns) that other VPNs often need workarounds. Keep texting if the test fails.

## Decision gate

Keep stock Seerr if the review paths work. Consider an original AI-assisted mockup if the review identifies a material usability problem that stock settings cannot resolve. Build the smallest useful extension only if the mockup solves that problem and the owner approves its maintenance and security scope. Keep credentials and approval enforcement on the server.

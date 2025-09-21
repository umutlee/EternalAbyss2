# CI Pipeline (PR split)

## Flows
- PR: "PR: PlayMode (Headless)" (tests only)
- main/tags: "Main: Build & Gate" (tests -> build)

## Secrets & Vars
- Secrets: UNITY_LICENSE (.ulf Base64, single line)
- Vars: UNITY_VERSION (match ProjectVersion.txt), PROJECT_PATH (default ".")

## Common Reds
- No valid license -> UNITY_LICENSE not passed or fork PR secrets blocked.
- Requested Unity version not found -> mismatch with ProjectVersion.txt.
- Editor API in PlayMode -> avoid EditorSceneManager/EnterPlayMode in PlayMode tests.

## Branch Protection
- Required checks: only "PR: PlayMode (Headless)". Build should not be required for PR.
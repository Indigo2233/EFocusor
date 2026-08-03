cmake_minimum_required(VERSION 3.16)
project(indi-efocuser LANGUAGES NONE)

# ── Compatible with indi-3rdparty top-level build ─────────────────────
# When building as a subdirectory of indi-3rdparty, the parent CMakeLists.txt
# already sets up cmake_modules/ path and includes GNUInstallDirs.
# Add this to indi-3rdparty/CMakeLists.txt:
#
#   option(WITH_EFOCUSER "Install EFucoser Focuser Driver" On)
#   ...
#   if(WITH_EFOCUSER)
#     add_subdirectory(indi-efocuser)
#   endif()

include(GNUInstallDirs)

# INDI data directory (where drivers.xml lives)
if(NOT DEFINED INDI_DATA_DIR)
    set(INDI_DATA_DIR "${CMAKE_INSTALL_DATADIR}/indi")
endif()

# ── Install driver XML ─────────────────────────────────────────────────

install(
    FILES drivers.xml
    DESTINATION "${INDI_DATA_DIR}"
)

# ── Install Python driver and wrapper ──────────────────────────────────

install(
    PROGRAMS
        indi_efocuser_focuser
        indi_efocuser_focuser.py
    DESTINATION "${CMAKE_INSTALL_BINDIR}"
)

# ── Install documentation ──────────────────────────────────────────────

install(
    FILES doc/index.md
    DESTINATION "${CMAKE_INSTALL_DATADIR}/doc/indi-efocuser"
    RENAME README.md
)

# ── Install license ────────────────────────────────────────────────────

install(
    FILES LICENSE
    DESTINATION "${CMAKE_INSTALL_DATADIR}/doc/indi-efocuser"
    RENAME COPYING
)

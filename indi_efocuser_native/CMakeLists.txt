cmake_minimum_required(VERSION 3.16)
project(indi_efucoser VERSION 1.0 LANGUAGES CXX)

find_package(INDI REQUIRED)

add_executable(indi_efucoser_focuser
    efucoser.cpp
    efucoser_protocol.cpp
)

target_compile_features(indi_efucoser_focuser PRIVATE cxx_std_17)
target_include_directories(indi_efucoser_focuser PRIVATE ${INDI_INCLUDE_DIR})
target_link_libraries(indi_efucoser_focuser PRIVATE ${INDI_LIBRARIES})

install(TARGETS indi_efucoser_focuser RUNTIME DESTINATION bin)

set(EFUCOSER_VERSION_MAJOR ${PROJECT_VERSION_MAJOR})
set(EFUCOSER_VERSION_MINOR ${PROJECT_VERSION_MINOR})
configure_file(efucoser.xml.cmake efucoser.xml @ONLY)
install(FILES ${CMAKE_CURRENT_BINARY_DIR}/efucoser.xml DESTINATION ${INDI_DATA_DIR})

include(CTest)
if(BUILD_TESTING)
    add_executable(efucoser_protocol_tests
        tests/efucoser_protocol_tests.cpp
        efucoser_protocol.cpp
    )
    target_compile_features(efucoser_protocol_tests PRIVATE cxx_std_17)
    target_include_directories(efucoser_protocol_tests PRIVATE ${CMAKE_CURRENT_SOURCE_DIR})
    add_test(NAME efucoser_protocol_tests COMMAND efucoser_protocol_tests)
endif()

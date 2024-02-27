BEGIN
   EXECUTE IMMEDIATE 'DROP TABLE ' || 'customer';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -942 THEN
         RAISE;
      END IF;
END;
GO
BEGIN
   EXECUTE IMMEDIATE 'DROP TABLE ' || 'seller';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -942 THEN
         RAISE;
      END IF;
END;
GO
BEGIN
  EXECUTE IMMEDIATE 'DROP PROCEDURE ' || 'testSP1';
EXCEPTION
  WHEN OTHERS THEN
    IF SQLCODE != -4043 THEN
      RAISE;
    END IF;
END;
GO
BEGIN
  EXECUTE IMMEDIATE 'DROP PROCEDURE ' || 'testSP2';
EXCEPTION
  WHEN OTHERS THEN
    IF SQLCODE != -4043 THEN
      RAISE;
    END IF;
END;
GO
BEGIN
  EXECUTE IMMEDIATE 'DROP PROCEDURE ' || 'testSP3';
EXCEPTION
  WHEN OTHERS THEN
    IF SQLCODE != -4043 THEN
      RAISE;
    END IF;
END;
GO
BEGIN
   EXECUTE IMMEDIATE 'DROP TABLE ' || 'customerkind';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -942 THEN
         RAISE;
      END IF;
END;
GO
BEGIN
   EXECUTE IMMEDIATE 'DROP TABLE ' || 'sellerkind';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -942 THEN
         RAISE;
      END IF;
END;
GO